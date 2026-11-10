# Rolling deployment baseline

`buyer-demo.yaml` supplies two replicas, maxUnavailable=0, readiness/live/startup probes, graceful drain and an API PodDisruptionBudget. Configure image registry tags, namespace, ingress/TLS, database and secrets for the target cluster before applying.

1. Build/push the API and web images, then replace their image tags in the manifest.
2. Bootstrap/upgrade the database using `database/CloneEbayBuyer.sql` (fresh databases only), demo seed if wanted, and `database/upgrades/001-buyer-completion.sql`.
3. Create Secret `buyer-secrets` with keys `connection-string` and `jwt-key`. Keep the same JWT key across replicas. Do not put real values in Git.
4. Configure `Frontend__PublicUrl`, allowed CORS origins, SMTP and Google OAuth as appropriate. OAuth across replicas additionally requires shared persistent ASP.NET Data Protection keys or correctly configured sticky routing; the baseline does not provision cluster storage.
5. Apply the manifest and watch `kubectl rollout status deployment/buyer-api`.
6. During a rolling update, run a continuous probe from outside the cluster and record any failed requests. A rolling strategy is readiness for a deployment test, not evidence that zero downtime has been achieved.

The SQL upgrade is additive except replacing money check constraints to include tax; it must run before rolling out the new API. Existing columns and rows remain compatible with the previous API during rollout. Long-lived SSE clients reconnect automatically when a pod drains.
