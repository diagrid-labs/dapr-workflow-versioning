---
layout: default
---

# Pattern: Child Workflows

- A parent workflow orchestrates one or more child workflows.
- Child workflows can run sequentially or in parallel.
- Each child workflow has its own state and execution history.

```mermaid
graph LR
    subgraph Parent Workflow
    direction LR
    Start([Input])
    Start --> FO{Fan-out}
    FO --> CW1[Child Workflow 1]
    FO --> CW2[Child Workflow 2]
    FO --> CW3[Child Workflow 3]
    CW1 --> FI{Fan-in}
    CW2 --> FI
    CW3 --> FI
    FI --> Join[Aggregate results]
    Join --> End([Output])
    end

    style Start fill:#e1f5ff
    style End fill:#d4edda
    style CW1 fill:#e8daef
    style CW2 fill:#e8daef
    style CW3 fill:#e8daef
```
