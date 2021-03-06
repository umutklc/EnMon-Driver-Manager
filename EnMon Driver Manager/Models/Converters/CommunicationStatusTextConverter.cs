﻿using System;
using System.ComponentModel;
using System.Globalization;

namespace EnMon_Driver_Manager.Models.Converters
{
    internal class CommunicationStatusTextConverter : BooleanConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)

        {
            if (value != null)
            {
                return (bool)value ? "Haberleşme Var" : "Haberleşme Yok";
            }
            return null;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)

        {
            return (string)value == "Haberleşme Var";
        }
    }
}