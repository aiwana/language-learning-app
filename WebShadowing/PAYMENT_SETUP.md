# Payment and subscription setup

The application supports MoMo and ZaloPay through `IPaymentService`. Prices and package periods are selected on the server; clients only submit the provider, billing period, and an idempotency key.

## Local and sandbox configuration

Keep merchant credentials out of committed JSON. Configure them with environment variables or .NET user secrets using these paths:

- `Payment:Momo:PartnerCode`, `AccessKey`, `SecretKey`, `RedirectUrl`, `IpnUrl`
- `Payment:ZaloPay:AppId`, `Key1`, `Key2`, `CallbackUrl`

Expose the local callback endpoint through an HTTPS tunnel when testing provider webhooks:

- MoMo: `/api/payment/webhooks/momo`
- ZaloPay: `/api/payment/webhooks/zalopay`

The checkout endpoints fail closed when credentials or HTTPS callback URLs are missing. Webhooks verify signatures and the stored server-side amount before activating VIP. Duplicate successful callbacks are idempotent.

## Production checklist

1. Replace sandbox endpoints only through production configuration.
2. Store credentials in the deployment secret manager.
3. Register the production HTTPS redirect and webhook URLs with the provider.
4. Disable `VipStub`; production defaults to disabled when the setting is absent.
5. Apply the existing payment/subscription schema migration before enabling checkout.
6. Monitor rejected callback counts and provider latency without logging signatures, tokens, or secrets.
