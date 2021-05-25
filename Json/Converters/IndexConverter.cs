//-----------------------------------------------------------------------
// <copyright file="IndexConverter.cs" company="Visual JSON Editor">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://visualjsoneditor.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SM_Layout_Editor.Converters
{
    public class IndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var list = (IList)values[1];
            if (list != null)
            {
                var index = list.IndexOf(values[0]) + 1;
                Debug.WriteLine(targetType == typeof(string) ? index.ToString() : (object)index);
                return targetType == typeof(string) ? index.ToString() : (object)index;
            }
            Debug.WriteLine(null);
            return null; 
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}