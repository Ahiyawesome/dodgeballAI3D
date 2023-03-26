import bpy
import mathutils as mt

bone = bpy.data.objects['Armature'].data.edit_bones["Lower Arm.L"]
bone_loc = mt.Vector((0, 0, 0))
bone.head = bone_loc
