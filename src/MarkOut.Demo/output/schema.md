# MarkOut Schema

## Shoe

```
Shoe (as document)
├─ Id: string → Field
├─ Model: string → Field
├─ Category: string → Field
├─ Price: decimal → Field
├─ InStock: bool → Field (yes/no)
├─ Features: List<MarkOut.Demo.Feature>? → Table
│  ├─ Name: string → Column
│  └─ Value: string → Column
└─ Reviews: List<MarkOut.Demo.Review>? → Table
   ├─ Author: string → Column
   ├─ Rating: int → Column
   └─ Comment: string → Column

Shoe (in table)
├─ Id: string → Column
├─ Model: string → Column
├─ Category: string → Column
├─ Price: decimal → Column
├─ InStock: bool → Column
├─ Features: List<MarkOut.Demo.Feature>? → Skipped (unsupported)
└─ Reviews: List<MarkOut.Demo.Review>? → Skipped (unsupported)
```

## InventoryEntry

```
InventoryEntry (as document)
├─ ShoeId: string → Field
├─ Size: string → Field
├─ Black: int → Field
├─ Green: int → Field
├─ Red: int → Field
└─ Total: int → Ignored

InventoryEntry (in table)
├─ ShoeId: string → Column
├─ Size: string → Column
├─ Black: int → Column
├─ Green: int → Column
├─ Red: int → Column
└─ Total: int → Ignored
```

## Feature

```
Feature (as document)
├─ Name: string → Field
└─ Value: string → Field

Feature (in table)
├─ Name: string → Column
└─ Value: string → Column
```

## Review

```
Review (as document)
├─ Author: string → Field
├─ Rating: int → Field
└─ Comment: string → Field

Review (in table)
├─ Author: string → Column
├─ Rating: int → Column
└─ Comment: string → Column
```
