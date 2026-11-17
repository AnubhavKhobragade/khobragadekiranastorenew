# GreenBasket — Grocery Store

A small ASP.NET Core 10 grocery store demo with product management, categories, cart, checkout, stock updates and order history. No external NuGet packages are required.

## Run locally

Install the .NET 10 SDK, then from this folder:

```bash
dotnet run
```

Open the local URL printed by ASP.NET Core (usually http://localhost:8080 or https://localhost:xxxx).

## Deploy free on Render

1. Create a GitHub repository and upload the contents of this folder.
2. Sign in to [Render](https://render.com/), choose **New → Blueprint**, and connect that repository.
3. Render reads `render.yaml`; select the free plan and deploy. The app gets an `*.onrender.com` address with HTTPS.
4. The first request after 15 minutes idle can take about a minute because free services sleep.

## Important data note

This starter stores data in `data/store.json`. Render free web services have an ephemeral filesystem, so product/order changes can be lost after a restart, sleep, or redeploy. This is suitable for a demo, not a live shop. For durable real-world orders, connect a persistent database (for example a managed PostgreSQL service) and review the host's current pricing/retention limits before launch.

## Scope / security

This is a starter/demo, not a production commerce system: it has no login/admin authentication, payment gateway, tax/shipping logic, or customer privacy workflow. Add authentication and a durable database before exposing management functions to the public.
