import bpy
import mathutils as mt

def main():
    index = 0
    with open(r"C:\Users\ahiya\BlenderStuff\Keypoints_3D_One.txt", "rt") as keypoints:
        for line in keypoints:
            bone = get_bone_type(index)
            if len(line) == 51 or len(line) == 52:
                type = 0
    #            print(line[2:49])
            elif len(line) == 48 or len(line) == 49:
                type = 1
    #            print(line[2:46])
    
            """ Testing just the first frame """
            else:
                break
            
            change_bone(type, line, bone)
            index += 1

    #bone = bpy.data.objects['Armature'].data.edit_bones["Lower Arm.L"]
    #bone_loc = mt.Vector((0, 0, 0))
    #bone.head = bone_loc

    print("WORKED")
    
def get_bone_type(index):
    pass
    # TODO: Find out which keypoint corresponds to which body part, then update this code
    
def change_bone(type, values, bone):
    if type == 0:
        x = values[2:17]
        y = values[18:33]
        z = values[34:49]
    elif type == 1:
        x = values[2:16]
        y = values[17:31]
        z = values[32:46]

if __name__ == "__main__":
    main()
