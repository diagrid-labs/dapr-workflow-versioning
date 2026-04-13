---
layout: default
---

# Dealing with breaking changes in workflows


## Initial workflow

```mermaid
graph LR
    A[App with Workflow V1]
    B[Workflow Engine]
    C[(State Store
    V1 state)]
    A <--> B
    B --> C
    C --> B
```

## After deploying V2

V1 workflows were not completed during deployment, they were 'in-flight'.
The workflow engine will try to complete the workflows but the V1 workflow state is incompatible with the V2 workflow.

```mermaid
graph LR
    A[App with Workflow V2]
    B[Workflow Engine]
    C[(State Store
    V1 state)]
    A <--> B
    B --> C
    C --> B
```
