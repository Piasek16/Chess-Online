# Chess Online

This is a Unity project, where I recreated the game of chess using the C# language and Unity Gaming Services.

## Features

- Online connection with another player
- Ability to connect using an IPv4 address or a short 6 character code
- Highlighting of possible moves
- Display of past moves in algebraic notation
- Ability to save a finished game into a PGN format

## How to play

To play the game, download it from the releases tab on the right or click [here](https://www.mediafire.com/file/ayc2vbdji3omsmd/ChessOnline.zip/file). Then unzip the downloaded file and launch `ChessOnline.exe`  
In order to inspect the game, you need to have Unity installed on your computer. You can download it from [here](https://unity.com/). Once you have Unity installed, open the cloned project folder and select `StartingScene.unity` from the Scenes folder. Lastly, click on the `Play` button to start the game. You can also build the game for different platforms using the Build Settings menu.

Once you launch the game client, you can proceed as follows:
1. Input your nickname and click `Login`
2. In the bottom left corner, choose whether to connect using an IPv4 address or a 6 character code (connecting through code is the default)
3. As a host player, click the `Start Host` button and give parameters for connecting to another player (the parameters will be displayed once you create the lobby)
4. As a client player, input the connection parameters received from the host player into an input field in the center and click `Connect`.
5. Once both players connect, each player can press the `Ready` button to declare their readiness to start the game.
6. When both players are ready, the host player can click the `Start Game` button in order to start the game.
7. The game will start and players will be able to make their moves on their respective turns.
8. Once one player checkmates another or a stalemate occurs, the game will end and a summary screen (with the option to save the game) will be displayed.
9. If one of the players wants to surrender the game to the opponent, they can do so by pressing the 'Esc' key and confirming their choice.
10. If you wish to play another game, repeat the steps 2 - 9, otherwise press the `Esc` key and click `Confirm` to close the game.

## Attribution

During the development of the game, the following assets were used:

- Game icon: [Chess icons created by Freepik - Flaticon](https://www.flaticon.com/free-icons/chess)
- Login screen background video: [Pawn](https://www.vecteezy.com/video/4475277-chess-pawn-particles-cinematic-background-video), [Knight](https://www.vecteezy.com/video/6435547-chess-knight-particles-cinematic-background-video), [Bishop](https://www.vecteezy.com/video/4475284-chess-bishop-particles-cinematic-background-video-hd-free-download), [Rook](https://www.vecteezy.com/video/4475279-rook-chess-strategy-game-cinematic-particles-motion-background-video)
- Login screen background music [Stockfish Chess music](https://www.youtube.com/watch?v=FrACO5QrqUI)
