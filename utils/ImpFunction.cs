namespace darsakApi.utils
{


    public class ImpFunction
    {

        public static string ToArabicNumbers(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace('0', '٠')
                .Replace('1', '١')
                .Replace('2', '٢')
                .Replace('3', '٣')
                .Replace('4', '٤')
                .Replace('5', '٥')
                .Replace('6', '٦')
                .Replace('7', '٧')
                .Replace('8', '٨')
                .Replace('9', '٩');
        }
    }
}