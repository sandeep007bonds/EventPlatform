#!/usr/bin/env bash
# One-time, by-hand install of Argo CD onto the AKS cluster created by
# infra/environments/dev. This is the single documented exception to the
# root CLAUDE.md's "no kubectl/helm by hand" rule: Argo CD is what makes
# every deploy AFTER this one GitOps-driven, so it can't deploy itself.
#
# Run once per cluster. Re-running is safe (kubectl apply is idempotent) but
# unnecessary unless upgrading ARGOCD_VERSION or re-registering the
# Application after deleting it.
set -euo pipefail

# Check https://github.com/argoproj/argo-cd/releases for a newer stable tag
# before running - Argo CD ships releases regularly and this default may be
# behind by the time you read this.
ARGOCD_VERSION="${ARGOCD_VERSION:-v2.13.2}"
ARGOCD_NAMESPACE="argocd"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> kubectl context: $(kubectl config current-context)"
read -r -p "Bootstrapping Argo CD onto the cluster above. Continue? [y/N] " confirm
[[ "${confirm}" == "y" || "${confirm}" == "Y" ]] || { echo "Aborted."; exit 1; }

echo "==> Creating ${ARGOCD_NAMESPACE} namespace"
kubectl create namespace "${ARGOCD_NAMESPACE}" --dry-run=client -o yaml | kubectl apply -f -

echo "==> Installing Argo CD ${ARGOCD_VERSION}"
kubectl apply -n "${ARGOCD_NAMESPACE}" \
  -f "https://raw.githubusercontent.com/argoproj/argo-cd/${ARGOCD_VERSION}/manifests/install.yaml"

echo "==> Waiting for argocd-server (this can take a few minutes on a small node pool)"
kubectl -n "${ARGOCD_NAMESPACE}" rollout status deployment/argocd-server --timeout=300s

echo "==> Registering the eventplatform-dev Application"
kubectl apply -f "${SCRIPT_DIR}/applications/dev.yaml"

echo
echo "==> If sandeep007bonds/EventPlatform is a PRIVATE repo, Argo CD cannot"
echo "    clone it yet - see 'Private repo access' in platform/argocd/README.md"
echo "    before the Application above will sync successfully."
echo
echo "==> Initial admin password (rotate this, or switch to SSO, before this"
echo "    cluster is anything but a personal dev sandbox):"
kubectl -n "${ARGOCD_NAMESPACE}" get secret argocd-initial-admin-secret \
  -o jsonpath='{.data.password}' | base64 -d
echo
echo "==> Reach the UI with: kubectl -n ${ARGOCD_NAMESPACE} port-forward svc/argocd-server 8080:443"
