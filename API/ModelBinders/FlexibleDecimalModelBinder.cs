using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.ModelBinders;

public class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var rawValue = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Task.CompletedTask;
        }

        if (TryParse(rawValue, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"The value '{rawValue}' is not valid.");
        }

        return Task.CompletedTask;
    }

    private static bool TryParse(string rawValue, out decimal parsed)
    {
        var trimmed = rawValue.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
        {
            return true;
        }

        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        var normalized = trimmed.Replace("\u00A0", string.Empty).Replace(" ", string.Empty);
        var hasComma = normalized.Contains(',');
        var hasDot = normalized.Contains('.');

        if (hasComma && !hasDot)
        {
            return decimal.TryParse(
                normalized.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out parsed);
        }

        if (hasDot && !hasComma)
        {
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
        }

        parsed = default;
        return false;
    }
}
