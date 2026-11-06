# Oddhatter-TameFollow

Press **Use (E)** on any tamed creature to toggle **follow / stay**, the way wolves already work: lox, asksvin, boar
and others. Before, those creatures could only be petted.

- Creatures whose AI has no follow behaviour keep being petted as normal. The BepInEx log lists each creature type
  the first time you use it ("can now be told to follow / stay" or "can't follow").
- Rename (Alt + Use) is unchanged.
- Client-side: only you need the mod, and it works on servers.

## Config (`BepInEx/config/com.oddhatter.tamefollow.cfg`)

| Setting | Default | Meaning |
|---|---|---|
| General.Enabled | true | false = vanilla (only wolves and other already-commandable creatures follow). |

Edits to the file apply while the game is running.

## Build

```
dotnet build -c Release -p:Deploy=true -p:Package=true
```

Deploys to the r2modman profile (`BepInEx/plugins/Oddhatter-TameFollow`) and writes `dist/Oddhatter-TameFollow-<version>.zip`.
