# opa-abac-authorization

### Références

Exemples de walk : https://stackoverflow.com/questions/64718594/using-walk-to-recursively-aggregate-resources-in-a-terraform-state-with-rego ; https://play.openpolicyagent.org/p/0K5cSyB6vi

Exemple de reachable (avec hiérarchie organisationnelle) : https://www.openpolicyagent.org/docs/latest/policy-reference/

Excellentes astuces avancées sur Rego : https://www.fugue.co/blog/5-tips-for-using-the-rego-language-for-open-policy-agent-opa

Plusieurs approches plutôt avancées et bien expliquées : https://medium.com/@agarwalshubhi17/rego-cheat-sheet-5e25faa6eee8

### Commandes de test

Pour lancer le serveur OPA :

```
docker run -d -p 8181:8181 --name opa openpolicyagent/opa run --server --addr :8181
```
Pour exécuter les policies sur les data, en les mettant à jour à chaque fois :
```
clear; curl --no-progress-meter -X PUT http://localhost:8181/v1/policies/app/abac --data-binary @policy.rego > /dev/null; curl --no-progress-meter -X PUT http://localhost:8181/v1/data --data-binary @data.json > /dev/null; curl --no-progress-meter -X POST http://localhost:8181/v1/data/app/abac --data-binary @input.json | jq -r '.result | .allow'
```
