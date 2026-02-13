using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace AuthApi.Extensions
{
    public static class OptionsBuilderFluentValidationExtensions
    {
        public static OptionsBuilder<TOptions> ValidateFluentValidation<TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
        {
            optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(
                provider => new FluentValidationOptions<TOptions>(
                    optionsBuilder.Name,
                    provider.GetRequiredService<IValidator<TOptions>>()));
            
            return optionsBuilder;
        }
        
        private class FluentValidationOptions<TOptions>(string? name, IValidator<TOptions> validator)
            : IValidateOptions<TOptions>
            where TOptions : class
        {
            public ValidateOptionsResult Validate(string? name1, TOptions? options)
            {
                // Null name is used to configure all named options.
                if (name != null && name != name1)
                {
                    return ValidateOptionsResult.Skip;
                }
                
                // Ensure options are provided to validate against
                if (options == null)
                {
                    return ValidateOptionsResult.Fail("Options instance is null");
                }
                
                var validationResult = validator.Validate(options);
                if (validationResult.IsValid)
                {
                    return ValidateOptionsResult.Success;
                }
                
                var errors = validationResult.Errors
                    .Select(error => $"Options validation failed for '{error.PropertyName}' with error: {error.ErrorMessage}")
                    .ToArray();
                
                return ValidateOptionsResult.Fail(errors);
            }
        }
    }
}
