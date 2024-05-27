# dodgeballAI3D
Using C# Unity and Blender in an attempt to create a 3D Dodgeball player AI from scratch, implementing LSTMs and Deep Learning.

# Purpose
To learn more about AI through practical means, and to teach people about AI. I presented my (incompleted) project to my [AP CS class](https://docs.google.com/presentation/d/1NSrqW7RetwKYoTIME1lFF-3Hm4K8RoBpHvzLD2diLNA/edit?usp=sharing), and I'm planning to make a YouTube video about this project once it is complete

# Method
I using a similar method to [this paper](https://arxiv.org/pdf/2105.12196). 

I use LSTMs to train 3 different actions (Imitation Learning). I use animations created via pose estimation (because I didn't know how to do motion capture).    

I then plug in these LSTMs into a deep convolutional neural network, and train them with Proximal Policy Optimization. 


