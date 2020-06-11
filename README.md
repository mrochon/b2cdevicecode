# Using OAuth2 Device Code flow with Azure AD B2C
This sample code allows use of [OAuth2 Device Authorization grant](https://tools.ietf.org/html/rfc8628) to obtain access tokens from Azure B2C. It is **not intended for production support:**: 
hardly tested and very little error handling code are the 'known problems'. Just a toy while we wait for the real thing!

To try it out navigate [to this html page](https://b2cdatastore.blob.core.windows.net/uix/TestPage.html) - it uses JS to get the token from B2C.

## Contents

The sample consists of two projects and html source for the sample page:
1. B2CDeviceCode web app providing two *OAuth2* endpoints (*device* and *token*) and html UI (*devicelogin*) to initiate user authentication with B2C. 
2. TestApp sample console application using MSAL.NET to AcquireTokenByDeviceCode API.
3. TestPage.html referenced above

## Operation

To use thi sample with your own B2C tenant:

1. Register both apps. The B2CDeviceLogin webapp needs to be registered as a public client with 
reply urls: 'https://[your url]/signin-oidc'. TesApp is configured to use 'app://devicecode'
2. Client application uses MSAL to request an access token. It is configured as if using ADFS - that's the only way I found to use MSAL
with a custom endpoint for requesting tokens.

