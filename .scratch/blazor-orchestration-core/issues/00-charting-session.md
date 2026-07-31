# Charting session: destination and orchestrator form

Type: grilling
Status: resolved

## Question

What is this effort finding its way to, and in what form does the orchestrator exist?

## Answer

**Destination:** decisions-locked. The map is complete when every open architectural decision for the
core package has an answer recorded in the package contract. Implementation is explicitly a later
effort, so tickets resolve decisions rather than deliver code.

**Orchestrator form:** a **skill**, comparable to the existing `blazor-component-architect` routing
skill. It owns routing, lane splitting, delegation, review, and aggregation. It is the only component
permitted to spawn sub-agents. Specialist sub-agents consult the packaged skills for implementation
guidance but do not delegate further.

**Scope:** all remaining work is planned in this effort, but the improvement-report work is charted
as a separate map so the two can produce independent specs.
