﻿using System;
using System.Linq;
using System.Reflection;

namespace NJsonSchema.DraftV4
{
    internal class JsonSchemaGenerator
    {
        public TSchemaType Generate<TSchemaType>(Type type)
            where TSchemaType : JsonSchemaBase, new()
        {
            var schema = new TSchemaType();

            var typeDescription = JsonObjectTypeDescription.FromType(type);
            schema.Type = typeDescription.Type;
            schema.Format = typeDescription.Format;

            if (schema.Type.HasFlag(JsonObjectType.Object))
            {
                schema.Title = type.Name;
                foreach (var property in type.GetRuntimeProperties())
                    LoadProperty(property, schema);
            }
            else if (schema.Type.HasFlag(JsonObjectType.Array))
            {
                schema.Type = JsonObjectType.Array;
                var itemType = type.GenericTypeArguments.Length == 0 ? type.GetElementType() : type.GenericTypeArguments[0];
                schema.Items = Generate<JsonSchemaBase>(itemType);
            }

            return schema;
        }

        private void LoadProperty<TSchemaType>(PropertyInfo property, TSchemaType parentSchema)
            where TSchemaType : JsonSchemaBase, new()
        {
            var propertyType = property.PropertyType;

            var jsonProperty = Generate<JsonProperty>(propertyType);
            parentSchema.Properties.Add(property.Name, jsonProperty);

            var attributes = property.GetCustomAttributes().ToArray();
            
            var propertyTypeDescription = JsonObjectTypeDescription.FromType(propertyType);
            if (propertyTypeDescription.IsAlwaysRequired ||
                attributes.Any(a => a.GetType().FullName == "System.ComponentModel.DataAnnotations.RequiredAttribute"))
                parentSchema.RequiredProperties.Add(property.Name);

            dynamic descriptionAttribute = attributes
                .FirstOrDefault(a => a.GetType().FullName == "System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null)
                jsonProperty.Description = descriptionAttribute.Description;

            dynamic regexAttribute = attributes
                .FirstOrDefault(a => a.GetType().FullName == "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
                jsonProperty.Pattern = regexAttribute.Pattern;

            dynamic rangeAttribute = attributes
                .FirstOrDefault(a => a.GetType().FullName == "System.ComponentModel.DataAnnotations.RangeAttribute");
            if (rangeAttribute != null)
            {
                if (rangeAttribute.Minimum != null)
                    jsonProperty.Minimum = rangeAttribute.Minimum;
                if (rangeAttribute.Maximum != null)
                    jsonProperty.Maximum = rangeAttribute.Maximum;
            }
        }
    }
}