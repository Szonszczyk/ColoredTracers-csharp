## Version 3.0.0 for SPT 4.0.*

New config is REQUIRED!

- The mod has been completely rewritten
- Changed the config file extension to .json
- Removed bullet background coloring; please use [Ammo Stats](https://forge.sp-tarkov.com/mod/167/ammo-stats) instead
- Removed tracer coloring based on bullet rating
- Re-added the option to use a 7-step color scale
- Added an option to assign a single color to specific ammunition types
- Added the InvertScale option, allowing the use of an inverted rainbow scale
- Changed mod load order to ``OnLoadOrder.PostSptModLoader + 3``

## Version 2.1.1 for SPT 4.0.*
- Added new line to config which can disable ColorConverterAPI check on server start

## Version 2.1.0 for SPT 4.0.*
- The entire mod has been rewritten
- Added background coloring for bullets
- Added tracer and background coloring based on a bullet’s rating within its caliber and category
- Removed the 7-step color scale in favor of a continuous rainbow scale based on penetration value or bullet rating
- Improved config loading to preserve the config file after mod updates
- Combined SameColor and SarynMode into a single mode

## Version 2.0.0 for SPT 4.0.*
- Initial release
- Removed Realism mod logic (for now)