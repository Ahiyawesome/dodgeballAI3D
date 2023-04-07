import bpy
from mathutils import Vector

# Maximum Location Difference
MLD = 0.02


def main():
    bpy.ops.object.mode_set(mode='POSE')
    scene, object, bones = initialize()
    index = 0
    cur_frame = 1
    with open(r"C:\Users\ahiya\BlenderStuff\Keypoints_3D_One.txt", "rt") as keypoints:
        for line in keypoints:
            bone = get_bone(index, bones)
            if len(line) == 51 or len(line) == 52:
                type = 0
            elif len(line) == 48 or len(line) == 49:
                type = 1
            else:
                object.keyframe_insert_by_name(type='LocRotScale')
                scene.frame_set(cur_frame)
                cur_frame += 1
                continue

            change_bone(type, line, bone)
            index += 1
            if index == 16:
                index = 0

    print("WORKED")


def initialize():
    object = bpy.ops.anim
    bones = ["Hip", "Upper Leg.R", "Lower Leg.R", "Foot.R", "Upper Leg.L", "Lower Leg.L", "Foot.L", "Spine", "Chest",
             "Neck", "Head", "Shoulder.L", "Upper Arm.L", "Lower Arm.L", "Shoulder.R", "Upper Arm.R", "Lower Arm.R"]
    frame_rate = 30
    scene = bpy.context.scene
    scene.frame_set(1)
    scene.render.fps = frame_rate
    return scene, object, bones


def get_bone(index, bones):
    bone = bones[index]
    bone = bpy.context.object.pose.bones[bone]
    return bone


def change_bone(type, values, bone):
    if type == 0:
        x = values[2:17]
        y = values[18:33]
        z = values[34:49]
    elif type == 1:
        x = values[2:16]
        y = values[17:31]
        z = values[32:46]

    """ If the value is positive, there's a space in front of the string """
    if x[0] == " ":
        x = x[1:]
    if y[0] == " ":
        y = y[1:]
    if z[0] == " ":
        z = z[1:]

    x, y, z = location_modifier(bone, x, y, z)

    bone.location = Vector((x, y, z))


def location_modifier(bone, x, y, z):
    x = float(x)
    y = float(y)
    z = float(z)

    if abs(x - bone.location.x) > MLD:
        if x > bone.location.x:
            x = bone.location.x + MLD
        else:
            x = bone.location.x - MLD

    if abs(y - bone.location.y) > MLD:
        if y > bone.location.y:
            y = bone.location.y + MLD
        else:
            y = bone.location.y - MLD

    if abs(z - bone.location.z) > MLD:
        if z > bone.location.z:
            z = bone.location.z + MLD
        else:
            z = bone.location.z - MLD

    return x, y, z


if __name__ == "__main__":
    main()
