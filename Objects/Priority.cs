using System;
using System.ComponentModel;
using Discord;

namespace MareJira.Objects;

public enum Priority {
    [Description("<:sango_01:1481753776745611505>")]
    Lowest = 1,
    [Description("<:sango_02:1481753799071633499>")]
    Low = 2,
    [Description("<:sango_03:1481753821637251112>")]
    Medium = 3,
    [Description("<:sango_06:1481753878834970654>")]
    High = 4,
    [Description("<:sango_05:1481753863194284032>")]
    Highest = 5
}

public static class EnumExtensionMethods {  
    public static string GetEnumDescription(this Enum enumValue) {  
        
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());  
        var descriptionAttributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);  
  
        return descriptionAttributes.Length > 0 ? descriptionAttributes[0].Description : enumValue.ToString();  
    }
    
    public static Color GetEnumColors(this Enum enumValue) {
        
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString()); 
        
        switch (fieldInfo.Name) {
            case "Lowest":
                return new (0x44786F);
            case "Low":
                return new (0x44786F);
            case "Medium":
                return new (0xBFA55F);
            case "High":
                return new (0xBFA55F);
            case "Highest":
                return new (0xFF312C);
            default:
                return new(0xFF312C);
        }
    }
}  