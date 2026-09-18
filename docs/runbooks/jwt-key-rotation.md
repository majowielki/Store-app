# Rotating the signing key of the access tokens

The identity service signs access tokens with `JwtSettings:SecretKey`; the gateway and every
service validate them with the same key. The key lives in Key Vault (`jwt-secret-key`) and
reaches the apps as a secret reference. Access tokens last 15 minutes; refresh tokens are random
values stored hashed in the identity database and do not depend on the key.

1. Generate a new key: `openssl rand -base64 48` (at least 32 characters).
2. Put it in the repository secret `JWT_SECRET_KEY` and run the CD workflow (or
   `az deployment group create` with the new value in the environment, see
   [deploy.md](deploy.md)). The template writes a new version of the Key Vault secret and the
   apps' new revisions read it.
3. Every access token issued with the old key is refused from that moment: a signed-in user gets
   one 401, the UI trades the refresh cookie for a new token and carries on; nobody signs in
   again. All apps switch in one deployment - a service left on the old key would refuse the new
   tokens.
4. Rotate `INTERNAL_API_KEY` the same way; the typed clients and the internal endpoints read it
   from the same secret, so one deployment switches both sides.

When a key is believed to be leaked, rotate first and then look at `store.auth.login.failed`
and the gateway's 401s; an attacker holding the old key can mint tokens only until step 2.
