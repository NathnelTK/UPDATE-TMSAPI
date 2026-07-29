namespace TmsApi.Application.DTOs;

// --- M7 Session 4 - Exercise 7: HATEOAS link representation ---
// Real REST APIs that adopt HATEOAS keep the link set small: self, next, prev for
// collections, plus one action link per resource — not seven. Decoupling the client
// from URL structure is the goal; replacing routing with hypermedia is not.
public record LinkDto(string Href, string Rel, string Method);