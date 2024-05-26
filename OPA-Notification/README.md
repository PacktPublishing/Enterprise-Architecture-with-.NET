# OPA rules to select notification channel (debug project)

To run the OPA server:
```
docker run -d -p 8182:8182 --name opa openpolicyagent/opa run --server --addr :8182
```

To execute the test on the policies, while updating them every time:
```
clear; curl --no-progress-meter -X PUT http://localhost:8182/v1/policies/notification --data-binary @policy.rego; curl --no-progress-meter -X PUT http://localhost:8182/v1/data --data-binary @data.json; curl --no-progress-meter -X POST http://localhost:8182/v1/data/notification --data-binary @input.json | jq -r '.result | .channel'
```
