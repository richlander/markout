# Selection ownership

**Status:** Proposed

This document defines the boundary between semantic result selection and
Markout presentation controls. It exists to keep service pagination, query
semantics, and rendering concerns from collapsing into one writer option.

## Decision

Markout renders a result chosen by its caller. It does not own semantic
pagination, result ranking, source acquisition, multi-source merging, stable
result identity, or command-line evaluation order.

A consumer may push semantic selection into a service call when that pushdown
is provably equivalent, but the consumer's results/query layer remains the
source of truth. Markout receives the selected rows after that decision.

## Layer boundary

| Layer | Responsibility |
| --- | --- |
| Service or data source | Execute supported filters, ordering, and pagination; report totals, exhaustion, and bounds honestly. |
| Consumer results/query layer | Define semantic selection, validate requests, preserve identity, and decide which operations may be pushed down. |
| Markout | Project and render the result supplied by the consumer. |

Pushdown is an optimization, not a transfer of semantic ownership. A service
request that cannot prove the same result must fetch a broader extent or remain
client-side.

## Markout controls

`MarkoutWriterOptions.RowWindow` is a single, table-local presentation lens. It
is useful when a caller deliberately asks Markout to render a head, tail, or
range of an already-defined table. It does not:

- establish a service acquisition bound;
- define a CLI's pagination grammar;
- assign or preserve semantic result addresses;
- compose filters, ranking, or multiple pagination stages;
- select equivalent items in formats that bypass Markout tables; or
- prove that an upstream source was exhausted.

`MarkoutWriterOptions.MaxItems` is presentation summarization. It caps rows
after Markout's table-local selection and may disclose omitted rows. It is not a
semantic result limit.

Markout column and field projection remains presentation-owned because it
changes the rendered shape without changing which semantic result rows the
consumer selected.

## Consumer requirements

Before rendering, a consumer that exposes semantic pagination must:

1. construct the complete semantic selection plan;
2. establish the ordering and identity domain that plan addresses;
3. determine which source operations are equivalent and safe to push down;
4. acquire enough data or completion evidence to validate the request;
5. apply any remaining selection in its results/query layer; and
6. send the same selected logical items to every output format.

Stable addresses must be typed row data assigned by the consumer. Markout may
render an address column, but it does not infer identity from displayed row
positions.

Rendered-line windows are downstream presentation operations because line
identity exists only after payload projection. They do not bound item
acquisition.

## When behavior belongs in Markout

A change belongs in Markout when all of the following are true:

- it is defined over Markout shapes or rendering state;
- it has the same meaning for every consumer;
- it can be honored consistently by every affected formatter; and
- it does not require source, query, ranking, or domain identity knowledge.

A change belongs in the consumer when any of those conditions is false.

When ownership is uncertain, write the owning design before beginning the
cross-repository source-reference loop. Exact-source consumer proof validates a
Markout design; it does not substitute for deciding whether Markout owns the
behavior.

## Co-development

When a design does require new Markout behavior:

1. land the Markout design first;
2. point the initiating consumer at the exact Markout source commit;
3. keep peer project-reference edits local and unpushed;
4. prove the behavior through the consumer's real output paths;
5. land and release Markout;
6. restore the consumer to the released package; and
7. only then open the consumer implementation PR.

When the ownership decision requires no Markout behavior change, no package
release or source-reference adoption is needed. The consumer proceeds in its
own repository.
