---
layout: default
---

# Dapr Workflow

- Dapr has a built-in workflow engine.
- Workflows are defined in code.
- Workflows are stateful, and (should be) deterministic.
- Activities are the the building blocks of a workflow, they contain non-deterministic code.

```mermaid
graph LR
    subgraph Workflow
    direction LR
    Start([Input])
    End([Output])
    Start --> A1[Activity 1]
    A1 --> IF{Condition}
    IF--false-->A2[Activity 2]
    IF--true-->A3[Activity 3]
    A2 --> End 
    A3 --> End
    end
    
    style Start fill:#e1f5ff
    style End fill:#d4edda
```
