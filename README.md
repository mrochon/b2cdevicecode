# Using OAuth2 Device Code flow with Azure AD B2C
AFAIK, B2C currently does not support the OAuth2 Device Code flow for requesting tokens in client apps with no access to a browser.
This sample implements such 'support'. This **not intended for production support:**: 
hardly tested, very little error handling code and some limitations mentioned below. I did it as mental
stretching exercise.

## Contents

The sample consists of two projects:
1. A web app providing two *OAuth2* endpoints (*device* and *token*) and html UI (*devicelogin*) to initiate user authentication with B2C. It does not need to be
reistered as an application in B2C. However, it's url needs to be used for reply url of the public client applications (see below).
2. A sample console application simulating a device code client.
3. For deployment, the web app needs access to a Redis cache. It is used for short-term caching of client requests

## Operation

1. Client application and API(s) need to be registered in B2C. The client needs to be registered as a public client with 
two reply urls: 'https://localhost:44358/signin-oidc' (or wherever you deploy the web app) and 'app://devicecode' (or similar) 
2. Client application uses MSAL to request an access token. It is configured as if using ADFS - that's the only way I found to use MSAL
with a custom endpoint for requesting tokens.

## Known limitations

1. Client can use MSAL.NET to request the tokens but must be configured as if using ADFS so that the request can be sent 
to the service provided here before 
2. The B2C user journey to be used for user signin/up can be set in the client app, when it requests the token. However, 
at this stage it needs to be also hardcoded in the web app. The value passed by the client will be ignored at this time (WIP).
2. Relies on B2C allowing a public client to have an https-schemed reply url.
4. The user, after entering the device code and authenticating is presented with an error html page. Can be ignored. The client should show the
access token shortly afterwards.
5. Token server is hard-coded to use my own B2C (mrochonb2cprod).
