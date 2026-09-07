# Usurper Reborn v1.1.2

The eight open design items, closed. Four were deferrals from the 1.1.1 bug
pass that needed a design rather than a patch; four were the oldest feature
requests on the tracker. Each was designed twice, by Codex working from the
source and by a Claude design agent working in the tree, reconciled against
the code (`DOCS/DESIGN_1.2_OPEN_ISSUES.md`), reviewed before a line was
written, and reviewed again after. Seven shipped here. The eighth, the Player
District, is designed and gets its own release.

## Groups

- **A follower who dies now dies properly.** A grouped player who fell in the
  leader's fight was only removed from it: no resurrection consumed, no death
  counted, and at next login the "saved dead" check took up to five levels and
  three quarters of their gold for the wrong reason. The death is now marked in
  the leader's fight and resolved on the follower's own session, with the same
  resurrection, permadeath, and soft-revive rules as a solo death, then the
  Temple. The mark is saved, so dropping the connection does not dodge it. The
  leader sees who fell, and fallen followers no longer show in the next fight.
- **Followers get their own ability cooldowns.** A follower of the leader's
  class shared the leader's per-fight cooldowns, blocking and being blocked by
  them.

## Town

- **Haggling has a way in.** The weapon and armor shops offer `[H]aggle` on the
  buy prompt. The rule is the original: your offer must be at least 80 percent
  of the price and within your Charisma tier (4 to 20 percent off). Three
  attempts per shop per day, and they now persist; they used to refill on
  every load. Being thrown out for pushing your luck is a bar until the next
  day, not "attempts are zero", which would have barred you the moment you
  spent the third one. Tax is charged on the agreed price, and you can try
  even if you can only afford the haggled price.
- **The bank vault is real.** It was one number for the whole server that
  reset to 500,000 on every restart, so a rich vault forgot itself overnight.
  It is now one persisted reserve per world, updated atomically online so two
  robbers cannot both take the same gold and a robber is paid exactly what was
  removed. Deposits are insured: no one's robbery touches your balance, and
  the survey says so. Robbery take is capped at 250,000; the reserve grows by
  25,000 plus one percent a day up to five million.

## Relationships

- **Neglect matters, but only time you were here counts.** Every seven days
  you were present without positive contact, an NPC who liked you better than
  Normal cools one step, down to Normal and never to hostility. A spouse stays
  married but their love wanes after a week's grace, and they say so at the
  door at seven and fourteen days. A month away costs nothing; the counter
  only moves on days you played. Any talk, gift, or time together resets the
  clock, and making up after a long silence gives a step back. The letter at
  three weeks and the spouse leaving at four are designed and held until the
  pacing has been seen live. (Issue #43.)
- **NPCs remember being left to die.** When a teammate or companion dies while
  you could have helped, they cool two steps toward you and remember it; a
  companion loses loyalty. "Could have helped" is judged against your last
  turn: they were already below half health when you chose, you held a usable
  healing potion or a learned heal you could afford, and you chose something
  else. Burst kills from full health are not your fault, and echoes,
  mercenaries, grouped players, arrests, and exhibitions are excluded. A
  Temple service to bring a fallen ally back quickly is the follow-up.
  (Issue #41.)

## Modding

- **Ability and spell numbers are moddable.** `GameData/abilities.json` and
  `spells.json` tune the built-in values by key, changing only what an entry
  sets; nothing is added or removed, so saves stay valid. A file with any
  problem is rejected whole with every problem logged. The editor exports both
  as templates. The repository's old `Data/` folder, which nothing read, is
  retired. (Issue #68.)

## Corrections

- The 1.1.1 notes said forty-four intimacy translations dropped the partner's
  name. Thirty-eight of them drop a pronoun that is deliberately empty in
  Spanish, French, Italian, and Hungarian. Two lines did drop the name; both
  are fixed, and a test now fails the build if a translation drops a
  placeholder English carries.

## Docker

- The compose stack was verified end to end and refreshed: the web image now
  ships everything the proxy serves (the language files and the Steam page
  were missing), runs on Node 22, and waits for the game server to create the
  database before starting; the proxy no longer serves its own source; the
  guide matches the code.

## Not in this release

The Player District (issue #97), designed for its own release. The 21-day
letter and 28-day leaving for neglect, and the Temple restore for a fallen
ally, both held for a second slice.

## Tests

1,074 passing, up from 1,019. New: `GroupCooldownTests`,
`GroupFollowerDeathTests`, `HagglingTests`, `BankVaultTests`,
`RelationshipNeglectTests`, `AllyAbandonmentTests`, `ModdableOverridesTests`,
and a placeholder parity check in `LocalizationIntegrityTests`.
