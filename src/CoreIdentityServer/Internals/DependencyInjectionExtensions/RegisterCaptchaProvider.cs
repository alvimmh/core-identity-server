// Copyright (c) VahidN. All rights reserved.
//
// By accessing the https://github.com/VahidN/DNTCaptcha.Core code here, you are agreeing to the following licensing terms:
// https://github.com/VahidN/DNTCaptcha.Core/blob/master/LICENSE.md
//
// or, alternatively you can view the license in the project's ~/Licenses/DNTCaptcha.CoreLicense.md file

using System;
using DNTCaptcha.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterCaptchaProvider
    {
        // Registers DNTCaptcha as the captcha provider for the application
        public static IServiceCollection AddCaptcha(this IServiceCollection services, IConfiguration config)
        {
            string captchaEncryptionKey = config["captcha_encryption_key"];

            if (string.IsNullOrWhiteSpace(captchaEncryptionKey))
                throw new NullReferenceException("Captcha encryption key is missing.");

            services.AddDNTCaptcha(options =>
            {
                // -> It doesn't rely on the server or client's times. Also it's the safest one.
                options.UseSessionStorageProvider()

                // -> It relies on the server's times. It's safer than the CookieStorageProvider.
                // options.UseMemoryCacheStorageProvider()

                // -> It relies on the server and client's times. It's ideal for scalability, because it doesn't save anything in the server's memory.
                // options.UseCookieStorageProvider(SameSiteMode.Strict)

                // --> It's ideal for scalability using `services.AddStackExchangeRedisCache()` for instance.
                // .UseDistributedCacheStorageProvider()
                // .UseDistributedSerializationProvider()

                // Don't set this line (remove it) to use the installed system's fonts (FontName = "Tahoma").
                // Or if you want to use a custom font, make sure that font is present in the wwwroot/fonts folder and also use a good and complete font!
                // This is optional.
                // .UseCustomFont(Path.Combine(_env.WebRootPath, "fonts", "IRANSans(FaNum)_Bold.ttf"))

                .AbsoluteExpiration(minutes: 7)
                .ShowThousandsSeparators(false)
                .WithNoise(baseFrequencyX: 0.015f, baseFrequencyY: 0.015f, numOctaves: 1, seed: 0.0f)
                .WithEncryptionKey(captchaEncryptionKey)
                
                // This is optional. Change it if you don't like the default names.
                .InputNames(
                    new DNTCaptchaComponent
                    {
                        CaptchaHiddenInputName = "DNTCaptchaText",
                        CaptchaHiddenTokenName = "DNTCaptchaToken",
                        CaptchaInputName = "DNTCaptchaInputText"
                    }
                )

                // This is optional. Change it if you don't like its default name.
                .Identifier("dntCaptcha");
     	    });

            return services;
        }
    }
}
