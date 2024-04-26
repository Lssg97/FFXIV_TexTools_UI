﻿using FFXIV_TexTools.Helpers;
using FFXIV_TexTools.ViewModels;
using FFXIV_TexTools.Views.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using xivModdingFramework.Cache;
using xivModdingFramework.Exd.FileTypes;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Items.Interfaces;
using xivModdingFramework.Materials.DataContainers;
using xivModdingFramework.Materials.FileTypes;
using xivModdingFramework.Mods;
using xivModdingFramework.SqPack.FileTypes;
using xivModdingFramework.Textures.Enums;
using static xivModdingFramework.Materials.DataContainers.ShaderHelpers;
using Button = System.Windows.Controls.Button;

namespace FFXIV_TexTools.Views.Textures
{
    public enum MaterialEditorMode
    {
        EditSingle,
        EditMulti,
        NewSingle,
        NewRace,
        NewMulti
    }
    /// <summary>
    /// Interaction logic for MaterialEditor.xaml
    /// </summary>
    public partial class MaterialEditorView
    {
        public class WrappedTexture : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public MtrlTexture Texture { get; set; }

            public MaterialEditorView View;

            public XivTexType Usage
            {
                get
                {
                    return Texture.Usage;
                }
            }

            public string TexturePath
            {
                get
                {
                    return View.Material.TokenizePath(Texture.TexturePath, Texture.Usage);
                }
                set
                {
                    Texture.TexturePath = View.Material.DetokenizePath(value, Texture.Usage);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TexturePath)));
                }
            }

            public WrappedTexture(MtrlTexture tex, MaterialEditorView view)
            {
                Texture = tex;
                View = view;
            }
        }

        private MaterialEditorViewModel viewModel;
        private IItemModel _item;
        private MaterialEditorMode _mode;

        public ObservableCollection<KeyValuePair<string, EShaderPack>> ShaderSource;

        public ObservableCollection<WrappedTexture> TextureSource;
        private static XivMtrl _copiedMaterial;

        private XivMtrl _Material;
        public XivMtrl Material
        {
            get
            {
                return _Material;
            }
        }

        public MaterialEditorView()
        {
            InitializeComponent();
            viewModel = new MaterialEditorViewModel(this);
            TextureSource = new ObservableCollection<WrappedTexture>();

            // Setup for the combo boxes.
            ShaderSource = new ObservableCollection<KeyValuePair<string, EShaderPack>>();
            ShaderComboBox.ItemsSource = ShaderSource;
            ShaderComboBox.DisplayMemberPath = "Key";
            ShaderComboBox.SelectedValuePath = "Value";

            var values = Enum.GetValues(typeof(EShaderPack));
            foreach(EShaderPack v in values)
            {
                var kv = new KeyValuePair<string, EShaderPack>(
                    ShaderHelpers.GetEnumDescription(v),
                    v
                );

                ShaderSource.Add(kv);
            }


            PasteMaterialButton.IsEnabled = _copiedMaterial != null;

            DisableButton.IsEnabled = false;
            DisableButton.Visibility = Visibility.Hidden;

            SaveButton.Click += SaveButton_Click;
        }

        public async Task<bool> SetMaterial(XivMtrl material, IItemModel item, MaterialEditorMode mode = MaterialEditorMode.EditSingle)
        {
            _Material = material;
            _item = item;
            _mode = mode;

            ShaderComboBox.SelectedValue = Material.Shader;
            UpdateTextureList();
            return await viewModel.SetMaterial(material, item, mode);
        }


        /// <summary>
        /// Updates the visual texture collection.
        /// </summary>
        public void UpdateTextureList()
        {
            TextureSource = new ObservableCollection<WrappedTexture>();
            foreach(var tx in Material.Textures)
            {
                var wt = new WrappedTexture(tx, this);
                TextureSource.Add(wt);
            }
            TexturesList.ItemsSource = TextureSource;
        }

        public XivMtrl GetMaterial()
        {
            return viewModel.GetMaterial();
        }

        public void Close(bool result)
        {
            DialogResult = result;
            Close();

        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _Material = await viewModel.SaveChanges();
            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }


        private void ShaderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShaderComboBox.SelectedValue == null)
            {
                return;
            }
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var help = new Views.Textures.MaterialEditorHelpView();
            help.ShowDialog();
        }

        private void CopyMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            _copiedMaterial = (XivMtrl) Material.Clone();
            PasteMaterialButton.IsEnabled = true;
        }

        private async void PasteMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (_copiedMaterial != null)
            {
                // Paste the copied Material into the editor using our current path and item.
                _copiedMaterial.MTRLPath = Material.MTRLPath;
                await SetMaterial(_copiedMaterial, _item, _mode);
            }
        }

        private async void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.DisableMod();
        }

        private void NewSharedButton_Click(object sender, RoutedEventArgs e)
        {
            var sharedTex = "{item_folder}/{default_name}";
        }

        private void NewUniqueButton_Click(object sender, RoutedEventArgs e)
        {
            var uniqueTex = "{item_folder}/{variant}_{default_name}";

        }

        private void TexturePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //var tb = (TextBox)sender;
            //var tex = (WrappedTexture)tb.DataContext;
            //tex.TexturePath = tb.Text;
        }

        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            var path = SavePresetDialog.ShowSavePresetDialog(Material.Shader, System.IO.Path.GetFileNameWithoutExtension(Material.MTRLPath));
            if (path == "")
            {
                return;
            }

            var _mtrl = new Mtrl(XivCache.GameInfo.GameDirectory);
            var bytes = _mtrl.CreateMtrlFile((XivMtrl)Material.Clone());
            System.IO.File.WriteAllBytes(path, bytes);
        }

        private async void LoadPresetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = LoadPresetDialog.ShowLoadPresetDialog(Material);
            if(result != null)
            {
                await SetMaterial(result, _item, _mode);
            }

        }

        private void EditShaderFlags_Click(object sender, RoutedEventArgs e)
        {
            var result = MaterialFlagsEditor.ShowFlagsEditor(Material, this);
            if(result)
            {
                // Don't really need to do anything here, since the editor handles updating the material.
            }
        }
        private void EditShaderKeys_Click(object sender, RoutedEventArgs e)
        {
            var result = ShaderKeysEditor.ShowKeysEditor(Material.ShaderKeys, this);
            if(result != null)
            {
                Material.ShaderKeys = result;
            }
        }

        private void EditShaderConstants_Click(object sender, RoutedEventArgs e)
        {
            var result = ShaderConstantsEditor.ShowConstantsEditor(Material.ShaderConstants, this);
            if(result != null)
            {
                Material.ShaderConstants = result;
            }
        }

        private void EditUsage_Click(object sender, RoutedEventArgs e)
        {
            var tex = (WrappedTexture)((Button)sender).DataContext;
            var result = TextureSamplerSettings.ShowSamplerSettings(Material, tex.Texture, this);
            if(result == true)
            {
                UpdateTextureList();
            }
        }
        
        private void RemoveTexture_Click(object sender, RoutedEventArgs e)
        {
            var tex = (WrappedTexture) ((Button)sender).DataContext;
            Material.Textures.Remove(tex.Texture);
            UpdateTextureList();
        }
        private void AddTexture_Click(object sender, RoutedEventArgs e)
        {
            var tex = new MtrlTexture();
            tex.Sampler = new TextureSampler();
            tex.Sampler.SamplerId = ESamplerId.g_SamplerNormal;
            Material.Textures.Add(tex);
            UpdateTextureList();
        }

        private async void LoadRawButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Material Files (*.mtrl)|*.mtrl|All Files (*.*)|*.*";
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            try
            {
                var _mtrl = new Mtrl(XivCache.GameInfo.GameDirectory);
                var bytes = File.ReadAllBytes(dialog.FileName);
                await SetMaterial(_mtrl.GetMtrlData(bytes, _Material.MTRLPath), _item, _mode);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show("Unable to load file:".L() + ex.Message, "Material Save Error".L(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveRawButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Material Files (*.mtrl)|*.mtrl|All Files (*.*)|*.*";
            dialog.FileName = Path.GetFileName(Material.MTRLPath);
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            try
            {
                var _mtrl = new Mtrl(XivCache.GameInfo.GameDirectory);
                var bytes = _mtrl.CreateMtrlFile(Material);
                File.WriteAllBytes(dialog.FileName, bytes);
            } catch(Exception ex)
            {
                FlexibleMessageBox.Show("Unable to save file:".L() + ex.Message, "Material Save Error".L(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
