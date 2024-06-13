# dodgeballAI3D
Using C# Unity and Blender in an attempt to create a 3D Dodgeball player AI from scratch, implementing LSTMs and Deep Learning.

# Purpose
To learn more about AI through practical means, and to teach people about AI. I presented my (incompleted) project to my [AP CS class](https://docs.google.com/presentation/d/1NSrqW7RetwKYoTIME1lFF-3Hm4K8RoBpHvzLD2diLNA/edit?usp=sharing), and I'm planning to make a YouTube video about this project once it is complete

# Method
I using a similar method to [this paper](https://arxiv.org/pdf/2105.12196). 

I use LSTMs to train 3 different actions (Imitation Learning). I use animations created via pose estimation (because I didn't know how to do motion capture). The inputs of the LSTMs are the x, y, and z values of the positions, rotations, velocities, and angular velocities of each of the 17 joints of the player. At first, I used the AI player for the inputs, but later I realized that the inputs of the target animation player are better becuase they are always the same for every epoch. The outputs are used to determine how much each joint should rotate (x, y, and z values for each joint). 
![](https://github.com/Ahiyawesome/dodgeballAI3D/blob/main/t1.gif)
![](https://github.com/Ahiyawesome/dodgeballAI3D/blob/main/lstm.gif).

These LSTMs are then used in a convolutional neural network to be trained in a Deep Learning process (this is the part that I'm currently working on). Since there are no targets, we create the gradient with a system of rewards, which is known as a policy gradient. The one that I will most likely use is the Proximal Policy Optimization.

# Resources Used
[Paper](https://arxiv.org/pdf/2105.12196) by Siqi Liu, Guy Lever, Zhe Wang, Josh Merel1, S. M. Ali Eslami, Daniel Hennes, Wojciech M.Czarnecki, Yuval Tassa, Shayegan Omidshafiei, Abbas Abdolmaleki, Noah Y. Siegel, LeonardHasenclever, Luke Marris, Saran Tunyasuvunakool, H. Francis Song, Markus Wulfmeier, Paul Muller, Tuomas Haarnoja, Brendan D. Tracey, Karl Tuyls, Thore Graepel1 and Nicolas Heess.

[Video ](https://www.youtube.com/watch?v=9JW41BNH9CM) by b2studios

[Article ](https://medium.com/@aidangomez/let-s-do-this-f9b699de31d9) by Aiden Gomez

[Paper](https://arxiv.org/abs/1707.06347) by John Schulman, Filip Wolski, Prafulla Dhariwal, Alec Radford, and Oleg Klimov.
[Paper]([https://arxiv.org/abs/1707.06347](https://openreview.net/forum?id=BJl6TjRcY7)) by Josh Merel, Leonard Hasenclever, Alexandre Galashov, Arun Ahuja, Vu Pham, Greg Wayne, Yee Whye Teh, and Nicolas Heess

 

