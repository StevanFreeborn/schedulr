# Notes

## Google Setup - Personal Google Account

- Go to the [Google Developers Console](https://console.developers.google.com/)
- Create a new project
- Enable the Google Calendar API
- Create a service account
  - Give the service account a name
  - Give the service account the role `Service Account Token Creator`
- Create a key for the service account
  - Go to the service account
  - Click on `Manage Keys`
  - Click on `Add Key`
  - Create a new key of type `JSON`
  - Save the key to a secure location
- Get the service account's email address (i.e. `something@something.iam.gserviceaccount.com`)
- Share the calendar you want to use with the service account's email address
  - Go to the calendar settings
  - Click on `Share with specific people`
  - Add the service account's email address
  - Give the service account `Make changes to events` permissions
  - Per [this issue](https://issuetracker.google.com/issues/148804709) you must manually accept the invitation to the calendar by making an [insert calendar list request](https://developers.google.com/calendar/api/v3/reference/calendarList/insert) with the service account
- Use the service account's key to authenticate requests to the Google Calendar API

## Google Authentication

- Create a JSON web token using the service account's key
- Use the JSON web token to get an access token
- Use the access token to make requests to the Google Calendar API

[source](https://developers.google.com/identity/protocols/oauth2/service-account)

### JSON Web Token

Needs headers:

```json
{
  "alg":"RS256",
  "typ":"JWT",
  "kid":"370ab79b4513eb9bad7c9bd16a95cb76b5b2a56a"
}
```

Needs payload with claims:

```json
{
  "iss": "761326798069-r5mljlln1rd4lrbhg75efgigp36m78j5@developer.gserviceaccount.com",
  "scope": "https://www.googleapis.com/auth/devstorage.read_only",
  "aud": "https://oauth2.googleapis.com/token",
  "exp": 1328554385,
  "iat": 1328550785
}
```

If using domain delegation - requires Google Workspace - then you need to include a `sub` claim:

```json
{
  "iss": "761326798069-r5mljlln1rd4lrbhg75efgigp36m78j5@developer.gserviceaccount.com",
  "sub": "some.user@example.com",
  "scope": "https://www.googleapis.com/auth/prediction",
  "aud": "https://oauth2.googleapis.com/token",
  "exp": 1328554385,
  "iat": 1328550785
}
```

Needs a signature. The input of which will be the base64url encoding of the header and payload. Need to use RSA using SHA-256 when computing the signature. Use the private key from the service account's key to sign the input.

```text
{"alg":"RS256","typ":"JWT"}.
{
"iss":"761326798069-r5mljlln1rd4lrbhg75efgigp36m78j5@developer.gserviceaccount.com",
"scope":"https://www.googleapis.com/auth/prediction",
"aud":"https://oauth2.googleapis.com/token",
"exp":1328554385,
"iat":1328550785
}.
[signature bytes]
```

This content is then base64url encoded and concatenated with periods:

```text
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiI3NjEzMjY3OTgwNjktcjVtbGpsbG4xcmQ0bHJiaGc3NWVmZ2lncDM2bTc4ajVAZGV2ZWxvcGVyLmdzZXJ2aWNlYWNjb3VudC5jb20iLCJzY29wZSI6Imh0dHBzOi8vd3d3Lmdvb2dsZWFwaXMuY29tL2F1dGgvcHJlZGljdGlvbiIsImF1ZCI6Imh0dHBzOi8vd3d3Lmdvb2dsZWFwaXMuY29tL29hdXRoMi92NC90b2tlbiIsImV4cCI6MTMyODU1NDM4NSwiaWF0IjoxMzI4NTUwNzg1fQ.UFUt59SUM2_AW4cRU8Y0BYVQsNTo4n7AFsNrqOpYiICDu37vVt-tw38UKzjmUKtcRsLLjrR3gFW3dNDMx_pL9DVjgVHDdYirtrCekUHOYoa1CMR66nxep5q5cBQ4y4u2kIgSvChCTc9pmLLNoIem-ruCecAJYgI9Ks7pTnW1gkOKs0x3YpiLpzplVHAkkHztaXiJdtpBcY1OXyo6jTQCa3Lk2Q3va1dPkh_d--GU2M5flgd8xNBPYw4vxyt0mP59XZlHMpztZt0soSgObf7G3GXArreF_6tpbFsS3z2t5zkEiHuWJXpzcYr5zWTRPDEHsejeBSG8EgpLDce2380ROQ
```

### Access Token

Need to make a POST request to `https://oauth2.googleapis.com/token` with the following parameters:

- `grant_type`: `urn:ietf:params:oauth:grant-type:jwt-bearer`
- `assertion`: The JSON web token

```shell
curl -d 'grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer&assertion=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiI3NjEzMjY3OTgwNjktcjVtbGpsbG4xcmQ0bHJiaGc3NWVmZ2lncDM2bTc4ajVAZGV2ZWxvcGVyLmdzZXJ2aWNlYWNjb3VudC5jb20iLCJzY29wZSI6Imh0dHBzOi8vd3d3Lmdvb2dsZWFwaXMuY29tL2F1dGgvcHJlZGljdGlvbiIsImF1ZCI6Imh0dHBzOi8vYWNjb3VudHMuZ29vZ2xlLmNvbS9vL29hdXRoMi90b2tlbiIsImV4cCI6MTMyODU3MzM4MSwiaWF0IjoxMzI4NTY5NzgxfQ.RZVpzWygMLuL-n3GwjW1_yhQhrqDacyvaXkuf8HcJl8EtXYjGjMaW5oiM5cgAaIorrqgYlp4DPF_GuncFqg9uDZrx7pMmCZ_yHfxhSCXru3gbXrZvAIicNQZMFxrEEn4REVuq7DjkTMyCMGCY1dpMa8aWfTQFt3Eh7smLchaZsU
' https://oauth2.googleapis.com/token
```

The response will contain the access token:

```json
{
  "access_token": "1/8xbJqaOZXSUZbHLl5EOtu1pxz3fmmetKx9W8CV4t79M",
  "scope": "https://www.googleapis.com/auth/prediction"
  "token_type": "Bearer",
  "expires_in": 3600
}
```

Can encounter errors. See [here](https://developers.google.com/identity/protocols/oauth2/service-account#error-codes) for more information.

### Making Requests

The access token can be used to make requests to Google APIs by including it as a bearer token in the `Authorization` header or as a query parameter.

```shell
curl -H "Authorization: Bearer access_token" https://www.googleapis.com/drive/v2/files

```

```shell
curl https://www.googleapis.com/drive/v2/files?access_token=access_token
```
