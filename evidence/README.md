# Evidence

Evidence is append-only and grouped by stage, candidate commit, and attempt.

```text
evidence/{stage}/{candidate}/{attempt-id}/
  input/
  output/
  logs/
  manifests/
  FINAL_VERDICT.json
  FINAL_VERDICT.md
  timeline.log
  END
```

Failed attempts must be retained. Self-test is not independent verification.
