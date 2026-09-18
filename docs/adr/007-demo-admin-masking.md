# 007 - The demo administrator sees masked data and may change nothing

**Status**: accepted

## Context

The store is shown publicly with a *Demo Admin* button: anybody can open the admin panel. The
panel lists customers and their orders. In the first version the demo administrator's
restrictions were scattered: an e-mail comparison in one controller, a check of the first role
claim in another, a 400 with "demo" in the message in a third, and a UI that hid buttons by
regular expression.

## Decision

Two admin roles. `true-admin` sees and changes everything. `demo-admin` may open every admin view,
but the order service masks the customers' names, e-mails and addresses in what it returns to
that role (`OrderMasking`), the identity service anonymises the user listing the same way, and
every change (`AdminWrite` policy: creating, editing or deleting a product) is refused with 403.
The UI learns the roles from `/auth/me` and shows the admin panel to both; it does not decide
anything by itself.

## Consequences

- The rule is enforced where the data is, once per service, by policy; the UI cannot get it wrong
  and a direct API call cannot get around it.
- Statistics stay real (revenue, counts, top products), since they reveal nobody.
- A demo administrator's session is a real session with a real token; only its role differs.
