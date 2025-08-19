# How to update the game

1. Install git by following this [guide](https://github.com/git-guides/install-git). You can also install a GUI program if you're not familiar with the command line https://git-scm.com/downloads/guis (recommend gitkraken).

2. Create an SSH key for your computer and add it to your Github account [Guide](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/adding-a-new-ssh-key-to-your-github-account)

3. Access the Github repository, and copy the **SSH** link [!Repo Link](repo-link.png)

4. Use that link either in your command line (e.g. `cd ~/Desktop && git clone git@github.com:Absolute-IT/InvasiveSpeciesGame.git`) or git client to clone the repo to your computer.

5. Make the changes you want to the game (further instructions below).

6. When you're ready to push the update, use the following process (or do the equivalent using your git client like gitkraken or git GUI):
  1. First add the files you've changed to the stage by navigating to the directory and running `git add .`
  2. Use `git commit -m "Describe your changes in here"` to set a message about what you changed. Make sure you replace the part in quotes with your message.
  3. Run `git push` to finalise the changes and push them to the repository.

7. Connect the touch tables to the internet using either Wi-Fi or Ethernet and wait up to 5 minutes. The update will automatically be pulled onto the touch tables through the crontab (see below). At this point, you can either restart the game or restart the touch table for the changes to kick in.

# How to make changes

The game is built using the Godot game development engine. It runs on any operating system, but make sure you install a version of 4.4.x to ensure it's compatible with the project. 

The project is surprisingly simple and easy to modify when you understand the structure.

1. All of the information is dynamically loaded from the game's `assets` and `config` directories. If you make changes to the files in these directories and follow the existing format, they will automatically be loaded into the game.

2. Check out the `documentation` directory for more information on specific systems. Particularly [Gallery](./documentation/Gallery.md), [Bug Squash Game](./documentation/BugSquashGame.md), [Memory Match Game](./documentation/MemoryMatchGame.md) and [Story Telling](./documentation/PowerPointStoryConverter.md). 

3. Much of the game's code is in the `scripts` directory. If you need to make changes to it and you're unfamiliar with programming, you can contact Absolute IT to arrange feature change jobs or bug fix jobs. If you'd like to try your hand at it yourself, you can try out Cursor which is a highly capable AI code editor https://cursor.com/home and can make any changes or fix bugs for you.

4. The [`assets`](./assets/) directory contains art work, powerpoint presentations and sound. By themselves, they don't do anything. You can use the files in [`config`](./config/) to specify which assets are loaded where. Take a look at the [Config Loader](./documentation/ConfigLoader.md) documentation for more detail on how to modify these files properly.

5. Once you've finished making your changes and tested them using Godot, simply push the changes to the repository. The touch tables will automatically compile them when they pull the update.

# Setup on Ubuntu 24.04

The following information is for setting up Ubuntu on the touch tables. This has already been done at the time of handover. But in the even that it needs to be redone for some reason, this is what you need to do.

## Need to use GNOME with disabled gestures plugin

1. Run `sudo apt-get install gnome-browser-connector`

2. Then navigate to https://extensions.gnome.org/extension/4049/disable-gestures-2021/

3. Install plugin using Shell version.

This should ensure that gestures are disabled so that multi-touch doesn't minimise the game.

## Setup a cron to auto-update the game

1. Run `crontab -e`

2. Add the following line `*/5 * * * *    cd ~/Desktop/InvasiveSpeciesGame && git pull`.

3. This will attempt pull any updates from the main branch ever 5 minutes. You just need to wait 5 minutes with the touch table connected to the internet until the update comes through and then restart the game.