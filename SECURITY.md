# Security Configuration Guide

## Environment Variables Setup

This application uses environment variables to store sensitive configuration data like JWT keys and encryption keys. **Never commit secrets to source control.**

### Development Setup

The project uses .NET User Secrets for development. Secrets are already configured if you cloned this repo, but if you need to set them up:

```bash
cd EasyPass.API
dotnet user-secrets set "Jwt:Key" "your-development-jwt-key-32-chars-min"
dotnet user-secrets set "Encryption:Key" "your-development-encryption-key"
```

### Production Deployment

#### Method 1: Environment Variables
Set these environment variables in your hosting environment:

```bash
# Required
export Jwt__Key="your-super-secure-jwt-key-at-least-32-characters-long"
export Encryption__Key="your-super-secure-encryption-key-for-aes256"
export ConnectionStrings__DefaultConnection="Server=your-server;Database=EasyPassDB;User Id=your-user;Password=your-password;"

# Optional
export Jwt__Issuer="EasyPass"
export Jwt__Audience="EasyPass"
export ASPNETCORE_ENVIRONMENT="Production"
```

#### Method 2: .env File (Not Recommended for Production)
1. Copy `.env.example` to `.env`
2. Fill in your production values
3. Ensure `.env` is in your `.gitignore`

### Docker Deployment

When using Docker, pass environment variables:

```bash
docker run -d \
  -e "Jwt__Key=your-jwt-key" \
  -e "Encryption__Key=your-encryption-key" \
  -e "ConnectionStrings__DefaultConnection=your-connection-string" \
  -p 5000:8080 \
  easypass-api
```

### Security Best Practices

1. **JWT Key**: Minimum 32 characters, cryptographically random
2. **Encryption Key**: Minimum 32 characters, cryptographically random  
3. **Rotation**: Regularly rotate keys in production
4. **Storage**: Use secure key management services (Azure Key Vault, AWS Secrets Manager, etc.)
5. **Access**: Limit who can access production secrets

### Key Generation

Generate secure keys using PowerShell:

```powershell
# Generate JWT Key (64 characters)
[System.Web.Security.Membership]::GeneratePassword(64, 10)

# Or using .NET crypto
[System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

### Troubleshooting

**Error: "JWT Key not found"**
- Development: Run `dotnet user-secrets list` to verify secrets are set
- Production: Verify environment variables are set correctly

**Error: "Encryption Key not found"** 
- Same as above - check user secrets (dev) or environment variables (prod)

**Invalid JWT Key Length**
- JWT keys must be at least 32 characters for security
- Use the key generation methods above