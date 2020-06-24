﻿using FFXIV_TexTools.Views.Textures;
using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xivModdingFramework.Materials.DataContainers;
using xivModdingFramework.Materials.FileTypes;
using xivModdingFramework.Textures.DataContainers;
using xivModdingFramework.Textures.Enums;

namespace FFXIV_TexTools.ViewModels
{
    public enum MaterialSpecularMode
    {
        FullColor,      // DXT1 Spec Map
        MultiMap,       // DXT1 Multi Map
        None            // None (Colorset Only)
    };

    public enum MaterialDiffuseMode
    {
        FullColor,      // DXT1 Diffuse Map
        None            // None (Colorset Only)
    }


    class MaterialEditorViewModel
    {
        private MaterialEditorView _view;
        private XivMtrl _material;
        public MaterialEditorViewModel(MaterialEditorView view)
        {
            _view = view;
        }

        public void SetMaterial(XivMtrl material)
        {
            _material = material;

            _view.MaterialPathLabel.Content = _material.MTRLPath;

            var shader = _material.GetShaderInfo();
            var normal = _material.GetMapInfo(XivTexType.Normal);
            var diffuse = _material.GetMapInfo(XivTexType.Diffuse);
            var specular = _material.GetMapInfo(XivTexType.Specular);
            var multi = _material.GetMapInfo(XivTexType.Multi);

            _view.NormalTextBox.Text = normal.path;

            _view.DiffuseComboBox.SelectedValue = MaterialDiffuseMode.None;
            _view.DiffuseTextBox.Text = "";
            if (diffuse != null)
            {
                _view.DiffuseComboBox.SelectedValue = MaterialDiffuseMode.FullColor;
                _view.DiffuseTextBox.Text = diffuse.path;
            }


            _view.SpecularComboBox.SelectedValue = MaterialSpecularMode.None;
            _view.SpecularTextBox.Text = "";

            if (multi != null)
            {
                _view.SpecularComboBox.SelectedValue = MaterialSpecularMode.MultiMap;
                _view.SpecularTextBox.Text = multi.path;
            }
            else if (specular != null)
            {
                _view.SpecularComboBox.SelectedValue = MaterialSpecularMode.FullColor;
                _view.SpecularTextBox.Text = specular.path;
            }

            _view.TransparencyComboBox.SelectedValue = shader.TransparencyEnabled;
            _view.ShaderComboBox.SelectedValue = shader.Shader;
            _view.NormalComboBox.SelectedValue = normal.Format;

        }

        public XivMtrl GetMaterial()
        {
            //return _materialSettings.Material;
            return _material;
        }

        /// <summary>
        /// Updates the XivMtrl with the selected changes.
        /// </summary>
        public void SaveChanges()
        {
            // Old Data
            var oldShader = _material.GetShaderInfo();
            var oldNormal = _material.GetMapInfo(XivTexType.Normal);
            var oldDiffuse = _material.GetMapInfo(XivTexType.Diffuse);
            var oldSpecular = _material.GetMapInfo(XivTexType.Specular);
            var oldMulti = _material.GetMapInfo(XivTexType.Multi);

            // New Data
            var newShader = new ShaderInfo() { Shader = (MtrlShader) _view.ShaderComboBox.SelectedValue, TransparencyEnabled = (bool) _view.TransparencyComboBox.SelectedValue };
            MapInfo newNormal = new MapInfo() { Usage = XivTexType.Normal, Format = (MtrlMapFormat) _view.NormalComboBox.SelectedValue, path = _view.NormalTextBox.Text };
            MapInfo newDiffuse = null;
            MapInfo newSpecular = null;
            MapInfo newMulti = null;


            // Specular
            if((MaterialSpecularMode) _view.SpecularComboBox.SelectedValue == MaterialSpecularMode.FullColor)
            {
                newSpecular = new MapInfo() { Usage = XivTexType.Specular, Format = MtrlMapFormat.WithoutAlpha, path = _view.SpecularTextBox.Text };
            } 
            else if((MaterialSpecularMode)_view.SpecularComboBox.SelectedValue == MaterialSpecularMode.MultiMap)
            {
                newMulti = new MapInfo() { Usage = XivTexType.Multi, Format = MtrlMapFormat.WithoutAlpha, path = _view.SpecularTextBox.Text };
            }

            // Diffuse
            if ((MaterialDiffuseMode)_view.DiffuseComboBox.SelectedValue == MaterialDiffuseMode.FullColor)
            {
                newDiffuse = new MapInfo() { Usage = XivTexType.Diffuse, Format = MtrlMapFormat.WithoutAlpha, path = _view.DiffuseTextBox.Text };
            }

            _material.SetShaderInfo(newShader);
            _material.SetMapInfo(XivTexType.Normal, newNormal);
            _material.SetMapInfo(XivTexType.Specular, newSpecular);
            _material.SetMapInfo(XivTexType.Multi, newMulti);
            _material.SetMapInfo(XivTexType.Diffuse, newDiffuse);

        }
    }
}
