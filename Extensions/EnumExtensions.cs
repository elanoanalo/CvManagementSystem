using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CvManagementSystem.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());

            var displayAttribute = field?
                .GetCustomAttribute<DisplayAttribute>();

            if (displayAttribute != null)
            {
                return displayAttribute.Name ?? enumValue.ToString();
            }

            return enumValue.ToString();
        }
    }
}