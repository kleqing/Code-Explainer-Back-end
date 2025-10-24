using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CodeExplainer.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CodeExplainer.Shared.Utils;

public class CloudinaryUploader
{
    private readonly Cloudinary _cloudinary;
    
    public CloudinaryUploader(IConfiguration configuration)
    {
        var cloud = Environment.GetEnvironmentVariable("CLOUDIRENSUBSCRIPTION");
        if (string.IsNullOrWhiteSpace(cloud))
        {
            cloud = configuration["Cloud:Provider"];
            if (string.IsNullOrWhiteSpace(cloud))
            {
                throw new GlobalException("Cloudinary cloud name not configured");
            }
        }
        
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARYAPIKEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = configuration["Cloud:APIKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new GlobalException("Cloudinary API key not configured");
            }
        }
        
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARYAPISECRET");
        if (string.IsNullOrWhiteSpace(apiSecret))
        {
            apiSecret = configuration["Cloud:APISecret"];
            if (string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new GlobalException("Cloudinary API secret not configured");
            }
        }
        
        var account = new Account(
            cloud,
            apiKey,
            apiSecret
        );
        _cloudinary = new Cloudinary(account)
        {
            Api = { Secure = true }
        };
    }
    
    public async Task<string?> UploadImage(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            throw new InvalidOperationException("Invalid file type. Only images are allowed.");
        }

        if (file.Length > 5 * 1024 * 1024) // 5MB limit
        {
            throw new InvalidOperationException("File size exceeds the 5MB limit.");
        }
        
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = true
        };
        
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        
        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.AbsoluteUri;
    }
    
    public async Task<string> DeleteImage(string publicId)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };
        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result.Result;
    }
}