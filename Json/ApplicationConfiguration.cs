//-----------------------------------------------------------------------
// <copyright file="ApplicationConfiguration.cs" company="Visual JSON Editor">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://visualjsoneditor.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace SM_Layout_Editor
{
    /// <summary>Stores the application configuration. </summary>
    public class ApplicationConfiguration
    {
        public ApplicationConfiguration()
        {
            IsFirstStart = true;
            
            WindowState = WindowState.Normal;

            Scale = 0;
            GridSize = 0;
            MoveSensitivity = 0;
            ZoomSensitivity = 0;

            RecentFiles = new ObservableCollection<RecentFile>();
        }

        public bool IsFirstStart { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public WindowState WindowState { get; set; }
        public ObservableCollection<RecentFile> RecentFiles { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Scale { get; set; }
        public int GridSize { get; set; }
        public int MoveSensitivity { get; set; }
        public int ZoomSensitivity { get; set; }
        public Tuple<int, int> Resolution { get; set; }
        public Tuple<Tuple<double, double>, Tuple<double, double>> Workspace { get; set; }
        public Tuple<double, double, double, double, double> SubWindowSizing { get; set; }
    }

    /// <summary>Describes a recently opened file. </summary>
    public class RecentFile
    {
        /// <summary>Gets or sets the file path. </summary>
        public string FilePath { get; set; }

        [JsonIgnore]
        public string FileName { get { return Path.GetFileName(FilePath); } }
    }
}