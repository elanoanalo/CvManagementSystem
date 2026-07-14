using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CvManagementSystem.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            // Берём поле enum по его значению
            var field = enumValue.GetType().GetField(enumValue.ToString());

            // Ищем атрибут [Display(Name = "...")]
            var displayAttribute = field?
                .GetCustomAttribute<DisplayAttribute>();

            // Если атрибут есть — возвращаем его Name
            // Если нет — возвращаем само название enum
            if (displayAttribute != null)
            {
                return displayAttribute.Name ?? enumValue.ToString();
            }

            return enumValue.ToString();
        }
    }
}