# opa-abac-authorization

### Companion blog article (in French)

The `README` allows a quick start of the project, but for the people who do not own the book and still want to understand a bit better what is going on in the code, there is a blog article (in French only) at https://blog.gouigoux.com/utiliser-open-policy-agent-pour-gerer-les-autorisations-complexes.html.

### References

Example of `walk`: https://stackoverflow.com/questions/64718594/using-walk-to-recursively-aggregate-resources-in-a-terraform-state-with-rego ; https://play.openpolicyagent.org/p/0K5cSyB6vi

Example of `reachable` (with organizational hierarchy): https://www.openpolicyagent.org/docs/latest/policy-reference/

Excellent expert hints on Rego: https://www.fugue.co/blog/5-tips-for-using-the-rego-language-for-open-policy-agent-opa

Several well-explained advanced approaches: https://medium.com/@agarwalshubhi17/rego-cheat-sheet-5e25faa6eee8

### Test commands

To run the OPA server:
```
docker run -d -p 8181:8181 --name opa openpolicyagent/opa run --server --addr :8181
```

To execute the test on the policies, while updating them every time:
```
clear; curl --no-progress-meter -X PUT http://localhost:8181/v1/policies/app/abac --data-binary @policy.rego > /dev/null; curl --no-progress-meter -X PUT http://localhost:8181/v1/data --data-binary @data.json > /dev/null; curl --no-progress-meter -X POST http://localhost:8181/v1/data/app/abac --data-binary @input.json | jq -r '.result | .allow'
```
