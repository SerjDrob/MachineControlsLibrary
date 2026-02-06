using System.Globalization;
using System.Windows;
using System.Windows.Input;
using HandyControl.Controls;

namespace MachineControlsLibrary.AttachedProperties;


public static class AttachedOverwriter
{
    private const int MaxDecimalDigits = 3;
    private static bool _isUpdating = false; // Защита от рекурсии
    public static string GetMyText(DependencyObject obj)
    {
        return (string)obj.GetValue(MyTextProperty);
    }

    public static void SetMyText(DependencyObject obj, string value)
    {
        obj.SetValue(MyTextProperty, value);
    }

    // Using a DependencyProperty as the backing store for MyText.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MyTextProperty =
        DependencyProperty.RegisterAttached("MyText", typeof(string), typeof(AttachedOverwriter), new PropertyMetadata("", OnMyTextChanged));

    private static void OnMyTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = d as WatermarkTextBox;
        if (box is not null)
        {
            box.PreviewTextInput += BoxTextChanged;
            box.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space) e.Handled = true;
            };
        }
    }

    private static void BoxTextChanged(object sender, TextCompositionEventArgs e)
    {
        var textBox = sender as WatermarkTextBox;
        string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (textBox == null || _isUpdating)
        {
            e.Handled = true;
            return;
        }

        var text = textBox.Text;
        var caretIndex = textBox.CaretIndex;

        // Разрешаем только цифры и точку
        if (!char.IsDigit(e.Text, 0) && e.Text != sep && e.Text != " ")
        {
            e.Handled = true;
            return;
        }

        // Разбиваем текст на две части: до и после каретки
        string beforeCaret = text.Substring(0, caretIndex);
        string afterCaret = text.Substring(caretIndex);

        string resultText;
        int newCaretIndex;

        if (e.Text == sep)
        {
            // Проверяем: уже есть точка?
            if (text.Contains(sep))
            {
                e.Handled = true;
                return;
            }

            // Вставляем точку
            resultText = beforeCaret + sep + afterCaret;
            newCaretIndex = caretIndex + 1;
        }
        else
        {
            // Это цифра
            char digit = e.Text[0];

            // Проверяем, где каретка: до или после точки
            int dotIndex = beforeCaret.IndexOf(sep);

            if (dotIndex == -1)
            {
                // До точки — режим insert (вставка)
                resultText = beforeCaret + digit + afterCaret;
                newCaretIndex = caretIndex + 1;
            }
            else
            {
                // После точки — режим replace (перезапись)
                int fractionalStart = dotIndex + 1;
                int posInFraction = caretIndex - fractionalStart;

                if (posInFraction >= MaxDecimalDigits)
                {
                    e.Handled = true;
                    return;
                }

                // Получаем текущую дробную часть (до каретки и после)
                string fractionalPart = text.Substring(fractionalStart);
                string newFractional;

                if (posInFraction < fractionalPart.Length)
                {
                    // Перезаписываем символ
                    if (fractionalPart.Length >= MaxDecimalDigits)
                    {
                        newFractional = fractionalPart.Substring(0, posInFraction) + digit +
                                        fractionalPart.Substring(posInFraction + 1, MaxDecimalDigits - posInFraction - 1);
                    }
                    else
                    {
                        // Дополняем, если ещё нет 3 цифр
                        newFractional = fractionalPart.Substring(0, posInFraction) + digit +
                                        (posInFraction + 1 <= fractionalPart.Length ? fractionalPart.Substring(posInFraction + 1) : "");
                    }
                }
                else
                {
                    // Расширяем дробную часть
                    newFractional = fractionalPart + digit;
                    if (newFractional.Length > MaxDecimalDigits)
                    {
                        e.Handled = true;
                        return;
                    }
                }

                // Собираем весь текст
                string integerPart = text.Substring(0, fractionalStart);
                resultText = integerPart + newFractional;

                newCaretIndex = caretIndex + 1;
                if (newCaretIndex - fractionalStart > MaxDecimalDigits)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // Проверяем, валиден ли результат
        if (IsValidDouble(resultText))
        {
            _isUpdating = true;
            textBox.Text = resultText;
            textBox.CaretIndex = newCaretIndex;
            _isUpdating = false;
            e.Handled = true;
        }
        else
        {
            e.Handled = true;
        }
    }
    private static bool IsValidDouble(string text) => double.TryParse(text, out double result);

}
