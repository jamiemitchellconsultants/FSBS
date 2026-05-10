#!/bin/bash
set -e

echo "Creating fsbs-documents bucket..."
awslocal s3 mb s3://fsbs-documents

echo "Setting CORS policy for direct browser uploads..."
awslocal s3api put-bucket-cors --bucket fsbs-documents --cors-configuration '{
  "CORSRules": [{
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:5000", "http://localhost:5001", "https://localhost:7001"],
    "AllowedMethods": ["GET", "PUT"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }]
}'

echo "LocalStack S3 ready."
