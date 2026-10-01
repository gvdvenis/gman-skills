---
name: use-plain-language
description: Plain-language rules for everything written to the user - explanations, reports, reviews, question rounds, proposed names and hand-backs. Loaded at session start by the hook that setup-gman-skills installs; invoke by hand to reload it.
disable-model-invocation: true
---

# Use plain language

The user knows technology well and reads each message once. Dense jargon, abstract noun
phrases and layered clauses make them re-read a passage several times, and a question that
needs a second read costs a whole round: they send it back and the decision waits.

Plain language changes the wording only. Keep every detail and every recommendation.

## Every message

- Say the thing directly. Three short sentences beat one dense one.
- Be concrete: the real file and line, the real command, the real number. Show the code or
  command itself.
- Define a term of art in the sentence where it first appears. Name things by what they
  measure, in ordinary words.
- Use words the user has used or `CONTEXT.md` defines. Where a new term seems needed, say what
  the code does instead.
- A trade-off with numbers gets a small comparison table.
- End a recommendation with what it costs.

## Question rounds

Grilling, wayfinder, domain-modeling, decision tables, lists of findings to pick from. The
skill that asks still sets the rounds and their order; this section sets the wording.

1. **Scene first.** Open with two or three sentences: what the topic is, why it comes up now,
   what is being decided.
2. **One example per question.** A code snippet, a sample input and output, or a before and
   after that makes the options visibly different.
3. **Earlier answers.** Read the user's earlier answers in this session and on the ticket
   first, and build on them. Ask again only when they conflict, and quote the earlier answer.
4. **Menus follow prose.** Before a pick-from menu (such as AskUserQuestion), explain each
   option in prose with one real example.

When no grilling skill is running, also number every question, spell out the options as
(a) / (b) / (c), and end each question with your recommended answer, so the user can reply
`Q3 b`.

## Names

When something needs a new name (glossary term, class, folder, build flag), propose it with
the current name and a one-line meaning beside it, then wait. The name goes into `CONTEXT.md`,
code or a file once the user picks it.

## Hand-back

When the turn ends waiting on the user, the last line says in one plain sentence what the user
must do, for example: "Run `dotnet run` in `src/Client` and tell me whether the page is still
white."

The agent does the work it can do itself: posting a ticket, calling an endpoint, drafting a
config file, checking a pipeline. The hand-back asks only for what needs the user: a decision,
a credential, or a check on their own machine.

## Before sending

Re-read the message as the user, once. Done when every question round opens with a scene,
every question has an example, every word is one the user knows, and a message that waits on
the user ends with the one thing they must do.
