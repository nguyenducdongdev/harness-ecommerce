#!/usr/bin/env bash
set -e

echo "=== Deploying Harness E-Commerce Platform to Kubernetes ==="

NAMESPACE="harness-ecommerce"

kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

echo "Applying Kubernetes manifests..."
kubectl apply -f deployment/k8s/configmap.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/secret.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/postgres.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/redis.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/rabbitmq.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/api-deployment.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/web-deployment.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/admin-deployment.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/ingress.yaml -n $NAMESPACE
kubectl apply -f deployment/k8s/hpa.yaml -n $NAMESPACE

echo "=== Deployment Applied Successfully ==="
kubectl get pods -n $NAMESPACE
