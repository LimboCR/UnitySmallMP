# UnitySmallMP
Unity 6 (6000.0.39f1) &lt;DX11> URP. Using Netcode for GameObjects, client-host system, server authority.

**Small description**
Project contains 2 main scene: MainMenu and GameWorld. MainMenu scene is responsible for starting Host and Clients. After host is started clients can connect to him and get into lobby. In lobby host can start a game or exit (forsing everybody to move to a GameWorld or disconnecting from the lobby). Client can only leave lobby.


# **Main menu demostration**

Main menu screen:
![image 9](https://github.com/user-attachments/assets/d5de8c69-4681-4fea-8856-e5f858ab1c50)


Lobby screen (host view):
![image 10](https://github.com/user-attachments/assets/adf2784b-b702-46f2-a875-4f19706165ff)


Lobby screen (client view):
![image 11](https://github.com/user-attachments/assets/1823164a-36f2-4f1d-8161-c4dc22479547)


# **Game World Demonstration**

**_Observation Camera_**

After the game is started, all players see observation camera (main camera) awaiting their spawn:
![image 12](https://github.com/user-attachments/assets/95632e7e-3a67-47f2-84c6-bed420b7d7af)


**_Player_**

After players spawn they are able to move using (WASD or gamepad), jump (space/GamePad x), look around with mouse(or gamepad), aim with right mouse button and shoot with left mouse button. Players hud display required data about their health, bullets, amount of kills and their nickname:
![image 13](https://github.com/user-attachments/assets/afaa7ddc-fae8-43ba-8099-8e10e0351fb0)


Player can die (friendly fire is on + bots can kill too). After he dies he goes back to observation camera and sees death screen for some time, before respawning:
![image 18](https://github.com/user-attachments/assets/c54baa4c-d7e2-43f0-96b3-8e966f91a960)


By pressing tab each player can view the ScoreTab to watch each other results:
![image 14](https://github.com/user-attachments/assets/dc13ab32-8f85-4217-ad86-e8212af67d29)
![image 15](https://github.com/user-attachments/assets/c513d130-0394-4cd2-b334-59b230a81743)


**_Map_**

Map is very simple, purely for demonstration purpouses, walled location with corridors, closed enviroments, obstacles and props. Mostly made for testing NavMeshAgent. It has 37 Props with physics and rigidbody (+ network object, network transform, network rigidbody and nav mesh obstacle) for testing load possibilities and syncing:
![image 16](https://github.com/user-attachments/assets/78405119-48bb-42fd-bb00-4afedd1e7299)


Map is fully prepared for navigation (baked):
![image 17](https://github.com/user-attachments/assets/5a0d3d27-2533-4c6c-96c6-9abe217e2464)

# Profiler Becnchmark (i5-7600 + GTX 1070, 24GB RAM, HardDrive)

Here are one of my profiler benchmarks:
![image 5](https://github.com/user-attachments/assets/0f7ac43f-6b28-4ec2-89d4-faecefb063f5)

![image 6](https://github.com/user-attachments/assets/7990d474-64cc-4e33-a2a8-fcf4ec167eaa)

![image 7](https://github.com/user-attachments/assets/28640398-dbb2-4f94-9e7f-cc34b19b4570)

![image 8](https://github.com/user-attachments/assets/9bf6e5a2-7e82-4929-8afd-c2a3c067927f)
