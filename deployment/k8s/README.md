# Graphite Kubernetes Deployment

This directory contains Kubernetes manifests for deploying Graphite to a cluster.

## Prerequisites

1. Kubernetes cluster with nginx ingress controller
2. cert-manager installed (for TLS)
3. GitHub Personal Access Token with `read:packages` scope

## Quick Start

### 1. Create GHCR Pull Secret

```bash
kubectl create secret docker-registry ghcr-secret \
  --namespace=graphite \
  --docker-server=ghcr.io \
  --docker-username=YOUR_GITHUB_USERNAME \
  --docker-password=YOUR_GITHUB_PAT
```

### 2. Update Configuration

Edit `secrets.yaml` with your actual values:
- `GitHub__ClientId` - GitHub OAuth App Client ID
- `GitHub__ClientSecret` - GitHub OAuth App Client Secret
- `GitHub__RedirectUri` - OAuth callback URL (e.g., `https://your-domain.com/api/auth/github/callback`)
- `GitHub__WebhookSecret` - GitHub webhook secret
- `Jwt__Key` - JWT signing key (32+ characters)
- `Frontend__Url` - Your frontend URL

Edit `ingress.yaml` and replace `your-domain.com` with your actual domain.

Edit `api-deployment.yaml` and `frontend-deployment.yaml` and replace `YOUR_GITHUB_USERNAME` with your GitHub username or organization.

### 3. Update Storage Class (if needed)

Edit `pvc.yaml` and change `storageClassName` to match your cluster's available storage class:

```bash
kubectl get storageclass
```

### 4. Deploy

```bash
# Apply all resources
kubectl apply -f namespace.yaml
kubectl apply -f configmap.yaml
kubectl apply -f secrets.yaml
kubectl apply -f pvc.yaml
kubectl apply -f api-deployment.yaml
kubectl apply -f api-service.yaml
kubectl apply -f frontend-deployment.yaml
kubectl apply -f frontend-service.yaml
kubectl apply -f ingress.yaml
```

Or apply all at once:
```bash
kubectl apply -f .
```

### 5. Verify Deployment

```bash
kubectl get all -n graphite
kubectl get ingress -n graphite
kubectl get certificates -n graphite
```

## Files

| File | Description |
|------|-------------|
| `namespace.yaml` | Creates the `graphite` namespace |
| `configmap.yaml` | Non-sensitive configuration |
| `secrets.yaml` | Sensitive configuration (template) |
| `pvc.yaml` | PersistentVolumeClaim for SQLite database |
| `api-deployment.yaml` | Backend API deployment |
| `api-service.yaml` | Backend API service |
| `frontend-deployment.yaml` | Frontend deployment |
| `frontend-service.yaml` | Frontend service |
| `ingress.yaml` | Ingress with TLS |
| `ghcr-secret.yaml` | GHCR credentials (template) |

## GitHub Actions Workflow

The workflow at `.github/workflows/build-deploy.yaml` automatically builds and pushes images to GHCR on push to `main` branch.

Images are tagged with both `latest` and the commit SHA.
