﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc;
using ChameleonForms.Attributes;
using ChameleonForms.Component.Config;
using ChameleonForms.Enums;

namespace ChameleonForms.FieldGenerators.Handlers
{
    internal class ListHandler<TModel, T> : FieldGeneratorHandler<TModel, T>
    {
        public ListHandler(IFieldGenerator<TModel, T> fieldGenerator, IFieldConfiguration fieldConfiguration)
            : base(fieldGenerator, fieldConfiguration)
        {}

        public override HandleAction Handle()
        {
            var model = FieldGenerator.GetModel();

            if (!FieldGenerator.Metadata.AdditionalValues.ContainsKey(ExistsInAttribute.ExistsKey) ||
                    FieldGenerator.Metadata.AdditionalValues[ExistsInAttribute.ExistsKey] as bool? != true ||
                    model == null)
                return HandleAction.Continue;

            // There is a bug in the unobtrusive validation for numeric fields that are a radio button
            //  when there is a radio button for "no value selected" i.e. value="" then it can't be selected
            //  as an option since it tries to validate the empty string as a number.
            // This turns off unobtrusive validation in that circumstance
            if (FieldConfiguration.DisplayType == FieldDisplayType.List && !FieldGenerator.Metadata.IsRequired && IsNumeric() && !HasMultipleValues())
                FieldConfiguration.Attr("data-val", "false");

            var selectList = GetSelectList(model);
            var html = GetSelectListHtml(selectList);
            return HandleAction.Return(html);
        }

        private IEnumerable<SelectListItem> GetSelectList(TModel model)
        {
            var propertyName = (string) FieldGenerator.Metadata.AdditionalValues[ExistsInAttribute.PropertyKey];
            var listProperty = model.GetType().GetProperty(propertyName);
            var listValue = (IEnumerable)listProperty.GetValue(model, null);
            if (listValue == null)
                throw new ListPropertyNullException(propertyName, FieldGenerator.GetPropertyName());
            return GetSelectListUsingPropertyReflection(
                listValue,
                (string)FieldGenerator.Metadata.AdditionalValues[ExistsInAttribute.NameKey],
                (string)FieldGenerator.Metadata.AdditionalValues[ExistsInAttribute.ValueKey]
            );
        }

        private IEnumerable<SelectListItem> GetSelectListUsingPropertyReflection(IEnumerable listValues, string nameProperty, string valueProperty)
        {
            foreach (var item in listValues)
            {
                var name = item.GetType().GetProperty(nameProperty).GetValue(item, null);
                var value = item.GetType().GetProperty(valueProperty).GetValue(item, null);
                yield return new SelectListItem { Selected = IsSelected(value), Value = value.ToString(), Text = name.ToString() };
            }
        }
    }
    
    /// <summary>
    /// Exception for when the list property for an [ExistsIn] is null.
    /// </summary>
    public class ListPropertyNullException : Exception
    {
        /// <summary>
        /// Creates a <see cref="ListPropertyNullException"/>.
        /// </summary>
        /// <param name="listPropertyName">The name of the list property that is null</param>
        /// <param name="propertyName">The name of the property that had the [ExistsIn] pointing to the list property</param>
        public ListPropertyNullException(string listPropertyName, string propertyName) : base(string.Format("The list property ({0}) specified in the [ExistsIn] on {1} is null", listPropertyName, propertyName)) {}
    }
}
