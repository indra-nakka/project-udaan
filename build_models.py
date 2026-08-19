"""
Udaan — headless Blender model builder.
Run via build-models.bat (Blender --background --python build_models.py) so it works
while you're away: it builds models, EXPORTS them into the Unity project's Assets,
and RENDERS preview PNGs into Udaan-Brain/blender-previews/ that Claude can read.

No MCP socket, no popups — just batch Blender. Expand build_*() per archetype over time.
"""
import bpy, os, math, mathutils

ROOT     = os.path.dirname(os.path.abspath(__file__))                 # ...\project-udaan
ASSETS   = os.path.join(ROOT, "udaan-client", "Assets", "Art", "Models")
PREVIEW  = os.path.join(ROOT, "Udaan-Brain", "blender-previews")
os.makedirs(ASSETS, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)

def log(msg): print("[BUILD] " + msg)

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, rgba):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf: bsdf.inputs["Base Color"].default_value = rgba
    m.diffuse_color = rgba          # workbench 'OBJECT'/'MATERIAL' preview color
    return m

def cube(name, loc, scale, material):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.name = name; o.scale = scale
    o.data.materials.append(material)
    return o

def cyl(name, loc, r, h, material, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=h, location=loc, vertices=12)
    o = bpy.context.active_object
    o.name = name; o.rotation_euler = rot
    o.data.materials.append(material)
    return o

def sphere(name, loc, scale, material, subdiv=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=1, location=loc)
    o = bpy.context.active_object
    o.name = name; o.scale = scale
    o.data.materials.append(material)
    return o

def torus(name, loc, major, minor, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(location=loc, major_radius=major, minor_radius=minor,
                                     major_segments=20, minor_segments=8)
    o = bpy.context.active_object
    o.name = name; o.rotation_euler = rot
    o.data.materials.append(material)
    return o

def cone(name, loc, r1, r2, h, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(radius1=r1, radius2=r2, depth=h, location=loc, vertices=4 if r2 == 0 and r1 > 0 else 16)
    o = bpy.context.active_object
    o.name = name; o.rotation_euler = rot
    o.data.materials.append(material)
    return o

def seg_between(name, p0, p1, r, material):
    """A thin cylinder connecting two points (for arcs, ribs, chains)."""
    v0 = mathutils.Vector(p0); v1 = mathutils.Vector(p1)
    mid = (v0 + v1) / 2.0; d = v1 - v0; length = max(d.length, 0.001)
    o = cyl(name, (mid.x, mid.y, mid.z), r, length, material)
    o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()   # align the cylinder's axis to the segment
    return o

# Art-Direction palette (see Udaan-Brain/Art-Direction.md)
BRASS  = (0.79, 0.63, 0.15, 1)
BRONZE = (0.55, 0.42, 0.25, 1)
CANVAS = (0.94, 0.89, 0.78, 1)
BLUE   = (0.30, 0.62, 1.00, 1)   # friendly accent / gameplay-readable
WOOD   = (0.61, 0.42, 0.24, 1)
PROPBLUR = (0.80, 0.82, 0.86, 0.5) # spinning-prop "blur" disk (translucent; run "Udaan > Make Prop Disks Translucent")
# Tier-0 junk palette
TIN    = (0.42, 0.40, 0.36, 1)
CARD   = (0.66, 0.48, 0.30, 1)
TAPE   = (0.30, 0.31, 0.33, 1)
TWINE  = (0.78, 0.68, 0.45, 1)

def join(parts, name):
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    obj.location = (0, 0, 0)
    return obj

def export(obj, name):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)   # clean transforms → identity
    fbx = os.path.join(ASSETS, name + ".fbx")
    # bake_space_transform bakes Blender's Z-up into Unity's Y-up so the model imports UPRIGHT at scale ~1
    # (no more per-axis stretching). Pairs with the Unity ModelPostprocessor (bakeAxisConversion).
    bpy.ops.export_scene.fbx(
        filepath=fbx, use_selection=True,
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        use_mesh_modifiers=True, object_types={'MESH'}, mesh_smooth_type='FACE')
    log("exported " + fbx)

def render(name, obj=None):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'   # show each material's colour
    scene.render.resolution_x = 640
    scene.render.resolution_y = 480
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")

    # Frame the object by its bounding box so tall/large props aren't cropped.
    center = mathutils.Vector((0, 0, 0.3)); maxd = 4.0
    if obj is not None:
        bpy.context.view_layer.update()
        corners = [obj.matrix_world @ mathutils.Vector(c) for c in obj.bound_box]
        center = sum(corners, mathutils.Vector()) / 8.0
        dims = obj.dimensions
        maxd = max(dims.x, dims.y, dims.z, 1.0)

    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    direction = mathutils.Vector((1.0, -1.0, 0.62)).normalized()   # 3/4 view
    cam.location = center + direction * (maxd * 1.9 + 3.0)
    d = center - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam
    bpy.ops.render.render(write_still=True)
    log("rendered " + scene.render.filepath)

# ── models ───────────────────────────────────────────────────────────────────
# NOTE: pipeline test — a simple low-poly player drone. Detailed per-archetype
# modelling (interceptor/sniper/bulwark/kamikaze/boss/core/outpost) lands on green-flag.
def build_player_drone():
    """Ghibli brass flying-machine: rounded pod + wooden canopy + one big top propeller + canvas wings.
       Friendly = ROUNDED/SMOOTH silhouette (see Art-Direction faction rule)."""
    brass  = mat("Brass", BRASS)
    wood   = mat("Wood", WOOD)
    canvas = mat("Canvas", CANVAS)
    blue   = mat("Accent", BLUE)
    parts = []
    parts.append(sphere("Pod", (0, 0, 0), (1.1, 1.6, 0.9), brass))            # rounded brass fuselage pod
    parts.append(sphere("Canopy", (0, 0.7, 0.55), (0.55, 0.6, 0.5), wood, 1)) # wooden cockpit dome
    parts.append(sphere("GlowEye", (0, 1.35, 0.35), (0.18, 0.18, 0.18), blue, 1)) # friendly blue lamp
    # short canvas wings, gently swept up
    parts.append(cube("WingL", (-1.15, -0.1, 0.1), (1.0, 0.7, 0.06), canvas))
    parts.append(cube("WingR", ( 1.15, -0.1, 0.1), (1.0, 0.7, 0.06), canvas))
    # big top propeller on a small mast
    parts.append(cyl("Mast", (0, 0, 0.9), 0.09, 0.6, wood))
    parts.append(cyl("Hub", (0, 0, 1.2), 0.16, 0.1, brass))
    parts.append(cube("Blade1", (0, 0, 1.25), (1.7, 0.16, 0.04), canvas))
    parts.append(cube("Blade2", (0, 0, 1.25), (0.16, 1.7, 0.04), canvas))
    # two small rear stabilizer rotors
    for sx in (-1, 1):
        parts.append(cyl(f"StabArm{sx}", (0.7 * sx, -1.2, 0), 0.07, 0.5, wood, (0, math.pi/2, 0)))
        parts.append(cyl(f"StabRotor{sx}", (1.05 * sx, -1.2, 0), 0.32, 0.05, brass))
    _blaster(parts, "enemy")   # visible toy blaster on the nose
    return join(parts, "Drone_Player")

def build_tier0_junk():
    """Tier 0 'The Scrapper' — a garbage-bin flying-machine held together with tape & twine.
       Still the FRIENDLY rounded silhouette, just janky and salvaged."""
    tin, card, tape, twine, blue = mat("Tin", TIN), mat("Card", CARD), mat("Tape", TAPE), mat("Twine", TWINE), mat("Lamp", BLUE)
    parts = []
    parts.append(cyl("Bin", (0, 0, 0), 1.0, 1.8, tin))                       # trash-can body
    parts.append(cyl("Lid", (0, 0, 1.0), 1.1, 0.18, tin))                    # dented lid
    parts.append(cube("TapeWrap", (0, 0, 0.1), (1.05, 1.05, 0.28), tape))    # tape around the middle
    parts.append(sphere("Bulb", (0, 0.95, 0.55), (0.16, 0.16, 0.16), blue, 1))  # salvaged lamp
    # cardboard wings, uneven
    parts.append(cube("WingL", (-1.2, -0.1, 0.0), (1.0, 0.7, 0.05), card))
    parts.append(cube("WingR", (1.15, 0.0, 0.15), (0.85, 0.6, 0.05), card))  # mismatched, higher
    # broom-handle mast + mismatched blades
    parts.append(cyl("Broom", (0.1, 0, 1.3), 0.06, 0.7, twine))
    parts.append(cube("BladeA", (0.1, 0, 1.6), (1.5, 0.14, 0.03), card))
    parts.append(cube("BladeB", (0.1, 0, 1.55), (0.12, 1.1, 0.03), tin))     # different size/material
    # crooked antenna + a taped patch
    parts.append(cyl("Antenna", (-0.6, 0.4, 1.1), 0.02, 0.8, twine, (0.3, 0.1, 0)))
    parts.append(cube("Patch", (0.7, 0.6, 0.2), (0.4, 0.02, 0.5), tape))
    _blaster(parts, "enemy")   # visible toy blaster on the nose
    return join(parts, "Drone_Tier0")

def build_quad_player():
    """PLAYER drone — a realistic DIY quadcopter (Tier 0): laser-cut plywood X-frame, gold motor
       bells, BIG black 2-blade props (believable lift ratio), exposed blue battery + green board.
       Cute + likable via chunky battery and a little front camera. Reference: user's DIY quad photos."""
    ply  = mat("Plywood", (0.72, 0.52, 0.30, 1))
    dark = mat("Arm",     (0.16, 0.16, 0.18, 1))
    bell = mat("Motor",   (0.86, 0.55, 0.12, 1))   # gold motor bell (like the refs)
    prop = mat("Prop",    (0.10, 0.10, 0.11, 1))   # black props
    batt = mat("Battery", (0.20, 0.52, 0.95, 1))   # blue battery
    board= mat("Board",   (0.13, 0.42, 0.20, 1))   # green PCB
    red  = mat("Wire",    (0.80, 0.16, 0.16, 1))
    parts = []
    d = 1.05  # motor offset per axis (X-config); arm ends land the motors at the frame tips
    a1 = cube("ArmA", (0, 0, 0), (0.16, 3.0, 0.05), dark); a1.rotation_euler = (0, 0, math.radians(45))
    a2 = cube("ArmB", (0, 0, 0), (0.16, 3.0, 0.05), dark); a2.rotation_euler = (0, 0, math.radians(-45))
    parts += [a1, a2]
    # central stack: frame plate + flight board + battery + strap
    parts.append(cube("Plate", (0, 0, 0.03), (0.85, 0.85, 0.06), ply))
    parts.append(cube("PCB",   (0, 0, 0.14), (0.55, 0.55, 0.05), board))
    parts.append(cube("Batt",  (0, 0, 0.30), (0.50, 0.75, 0.28), batt))
    parts.append(cube("Strap", (0, 0, 0.30), (0.56, 0.18, 0.30), red))
    parts.append(cube("Cam",   (0, 0.55, 0.02), (0.16, 0.16, 0.14), dark))   # tiny front camera
    # motors + big props at the 4 tips
    for (sx, sy) in [(1, 1), (1, -1), (-1, 1), (-1, -1)]:
        mx, my = d * sx, d * sy
        parts.append(cyl(f"Mount{sx}{sy}", (mx, my, 0.04), 0.18, 0.10, ply))
        parts.append(cyl(f"Motor{sx}{sy}", (mx, my, 0.16), 0.14, 0.18, bell))
        parts.append(cyl(f"Hub{sx}{sy}",   (mx, my, 0.28), 0.06, 0.05, prop))
        parts.append(cyl(f"PropDisk{sx}{sy}", (mx, my, 0.29), 0.85, 0.03, mat("PropBlur", PROPBLUR)))   # spin-blur disk
    # simple landing legs
    for (sx, sy) in [(1, 1), (-1, -1)]:
        parts.append(cyl(f"Leg{sx}{sy}", (0.5 * sx, 0.5 * sy, -0.28), 0.04, 0.6, dark))
    _blaster(parts, "foam")
    return join(parts, "Drone_Player_T0")

def _xframe(parts, dark, span=2.9, thick=0.14, zh=0.06):
    a1 = cube("ArmA", (0, 0, 0), (thick, span, zh), dark); a1.rotation_euler = (0, 0, math.radians(45))
    a2 = cube("ArmB", (0, 0, 0), (thick, span, zh), dark); a2.rotation_euler = (0, 0, math.radians(-45))
    parts += [a1, a2]

def _props4(parts, d, motor_mat, prop_mat, prad, z=0.26, guard_mat=None, guard_r=None):
    for sx, sy in [(1, 1), (1, -1), (-1, 1), (-1, -1)]:
        mx, my = d * sx, d * sy
        parts.append(cyl(f"M{sx}_{sy}", (mx, my, z - 0.12), 0.12, 0.16, motor_mat))
        parts.append(cyl(f"PD{sx}_{sy}", (mx, my, z), prad, 0.03, mat("PropBlur", PROPBLUR)))   # spin-blur disk
        if guard_mat and guard_r:
            parts.append(torus(f"G{sx}_{sy}", (mx, my, z), guard_r, 0.05, guard_mat))

def build_quad_t2():
    """Tier 2 Consumer (DJI Neo-like): clean molded shell, INTEGRATED prop guards, tiny cam."""
    shell = mat("Shell", (0.62, 0.64, 0.66, 1)); dark = mat("Trim", (0.20, 0.20, 0.22, 1))
    prop = mat("Prop", (0.12, 0.12, 0.13, 1)); blue = mat("LED", BLUE)
    parts = [sphere("Body", (0, 0, 0), (1.0, 1.3, 0.45), shell)]
    parts.append(cube("Cam", (0, 1.15, -0.05), (0.22, 0.24, 0.20), dark))
    _xframe(parts, dark, span=2.2, thick=0.1)
    _props4(parts, 1.1, dark, prop, 0.6, z=0.22, guard_mat=shell, guard_r=0.72)   # integrated guards
    parts.append(sphere("LED", (0, -1.05, 0.1), (0.1, 0.1, 0.1), blue, 1))
    _blaster(parts, "clip")
    return join(parts, "Drone_Player_T2")

def build_quad_t3():
    """Tier 3 Prosumer (DJI Mini/Mavic-like): aero body, folding-arm look, 3-axis GIMBAL ball, sensor eyes."""
    shell = mat("Shell", (0.30, 0.31, 0.34, 1)); dark = mat("Trim", (0.12, 0.12, 0.14, 1))
    prop = mat("Prop", (0.10, 0.10, 0.11, 1)); glass = mat("Lens", (0.05, 0.08, 0.12, 1)); blue = mat("LED", BLUE)
    parts = [cube("Body", (0, 0, 0), (0.9, 1.7, 0.42), shell), cube("Nose", (0, 1.0, 0), (0.7, 0.8, 0.4), shell)]
    parts.append(sphere("Gimbal", (0, 1.35, -0.18), (0.28, 0.28, 0.28), dark, 1))   # the premium cue
    parts.append(sphere("Lens", (0, 1.58, -0.18), (0.12, 0.12, 0.10), glass, 1))
    for sx in (-1, 1):
        parts.append(sphere(f"Eye{sx}", (0.3 * sx, 1.45, 0.05), (0.06, 0.06, 0.06), glass, 1))  # obstacle sensors
    _xframe(parts, dark, span=2.7, thick=0.12, zh=0.1)
    _props4(parts, 1.05, dark, prop, 0.62, z=0.2)
    parts.append(sphere("LED", (0, -1.1, 0.08), (0.09, 0.09, 0.09), blue, 1))
    _blaster(parts, "clip")
    return join(parts, "Drone_Player_T3")

def build_quad_t5():
    """Tier 5 Ascendant (Skydio/ducted-concept): graphite body, DUCTED rotors, sensor cluster, glowing seams."""
    body = mat("Graphite", (0.14, 0.14, 0.16, 1)); duct = mat("Duct", (0.10, 0.10, 0.12, 1))
    glow = mat("Seam", (0.30, 0.70, 1.0, 1)); glass = mat("Lens", (0.04, 0.06, 0.10, 1))
    parts = [sphere("Core", (0, 0, 0), (1.2, 1.5, 0.5), body), sphere("Sensor", (0, 0, 0.45), (0.42, 0.42, 0.3), body, 1)]
    for i, ang in enumerate((0, 120, 240)):
        rx, ry = 0.28 * math.cos(math.radians(ang)), 0.28 * math.sin(math.radians(ang))
        parts.append(sphere(f"Eye{i}", (rx, ry, 0.62), (0.09, 0.09, 0.09), glass, 1))
    parts.append(sphere("Gimbal", (0, 1.15, -0.15), (0.3, 0.3, 0.3), body, 1))
    parts.append(sphere("GLens", (0, 1.4, -0.15), (0.13, 0.13, 0.11), glass, 1))
    for sx, sy in [(1, 1), (1, -1), (-1, 1), (-1, -1)]:
        mx, my = 1.15 * sx, 1.15 * sy
        parts.append(torus(f"Duct{sx}{sy}", (mx, my, 0.05), 0.62, 0.12, duct))       # ducted/enclosed rotor
        parts.append(cyl(f"Fan{sx}{sy}", (mx, my, 0.05), 0.5, 0.03, body))
        parts.append(cube(f"FanSeam{sx}{sy}", (mx, my, 0.06), (1.0, 0.05, 0.04), glow))
    parts.append(cube("BodySeam", (0, 0, 0.5), (0.1, 2.4, 0.03), glow))
    _blaster(parts, "emitter")
    return join(parts, "Drone_Player_T5")

def build_core():
    """Defend objective — warm reactor crystal in a cradle, with the tall gameplay-readable blue beacon beam."""
    housing = mat("CoreBrass", (0.60, 0.50, 0.30, 1)); crystal = mat("Crystal", (0.30, 0.70, 1.0, 1)); beamm = mat("Beam", (0.30, 0.62, 1.0, 1))
    parts = [cyl("Cradle", (0, 0, 0), 1.2, 0.6, housing), sphere("Crystal", (0, 0, 0.9), (0.7, 0.7, 0.9), crystal, 2)]
    parts.append(cyl("Beam", (0, 0, 6.0), 0.2, 12.0, beamm))
    return join(parts, "Core")

def build_outpost():
    """Capture beacon — a little windmill/beacon tower (brass frame + canvas sails + owner orb on top)."""
    frame = mat("Frame", (0.55, 0.42, 0.25, 1)); sail = mat("Sail", (0.92, 0.88, 0.78, 1)); orb = mat("Orb", (0.95, 0.95, 0.98, 1))
    parts = [cyl("Post", (0, 0, 1.3), 0.18, 2.6, frame)]
    for ang in (0, 90, 180, 270):
        b = cube(f"Sail{ang}", (0, 0, 2.4), (1.3, 0.5, 0.04), sail); b.rotation_euler = (0, 0, math.radians(ang))
        parts.append(b)
    parts.append(sphere("Orb", (0, 0, 3.0), (0.5, 0.5, 0.5), orb, 2))
    return join(parts, "Outpost")

def build_boss_octa():
    """Final boss — a menacing PURPLE octacopter (8 rotors): bulky hull, evil red eye, a scary chin weapon pod."""
    body = mat("BossBody", (0.45, 0.15, 0.55, 1))   # purple
    dark = mat("BossArm",  (0.12, 0.10, 0.16, 1))
    prop = mat("BossProp", (0.10, 0.10, 0.12, 1))
    glow = mat("BossEye",  (1.0, 0.12, 0.12, 1))     # evil red
    parts = [sphere("Hull", (0, 0, 0), (1.9, 1.9, 0.8), body), sphere("Dome", (0, 0, 0.55), (1.0, 1.0, 0.6), body, 1)]
    parts.append(sphere("Eye", (0, 1.3, 0.2), (0.32, 0.32, 0.32), glow, 1))          # red eye
    parts.append(cube("WeaponPod", (0, 1.15, -0.5), (0.7, 0.8, 0.5), dark))          # scary chin weapon
    parts.append(cyl("Barrel1", (-0.22, 1.6, -0.5), 0.1, 0.6, dark, (math.radians(90), 0, 0)))
    parts.append(cyl("Barrel2", ( 0.22, 1.6, -0.5), 0.1, 0.6, dark, (math.radians(90), 0, 0)))
    # 4 crossed arm-beams → 8 radiating tips, motor + prop at each tip
    for a in (0, 45, 90, 135):
        b = cube(f"Arm{a}", (0, 0, 0), (0.16, 4.0, 0.1), dark); b.rotation_euler = (0, 0, math.radians(a)); parts.append(b)
    for i in range(8):
        ang = math.radians(i * 45)
        ax, ay = math.cos(ang) * 2.0, math.sin(ang) * 2.0
        parts.append(cyl(f"BMotor{i}", (ax, ay, 0.08), 0.15, 0.16, body))
        parts.append(cyl(f"BPropDisk{i}", (ax, ay, 0.24), 0.6, 0.03, mat("PropBlur", PROPBLUR)))   # spin-blur disk
    return join(parts, "Boss_Octa")

def _blaster(parts, style):
    """Playful NON-LETHAL toy blaster on the nose (see Design-Weapons-and-Tools.md). Kept small so the quad stays the hero."""
    if style == "foam":       # T0: taped foam-dart popper
        tube = mat("FoamTube", (0.85, 0.75, 0.55, 1)); band = mat("Band", (0.80, 0.16, 0.16, 1))
        parts.append(cyl("BlasterTube", (0, 0.95, -0.32), 0.12, 0.5, tube, (math.radians(90), 0, 0)))
        parts.append(cyl("BlasterBand", (0, 0.72, -0.32), 0.14, 0.08, band, (math.radians(90), 0, 0)))
    elif style == "clip":     # mid tiers: clip-on colourful blaster
        body = mat("Blaster", (0.20, 0.20, 0.24, 1)); tip = mat("BTip", (0.90, 0.60, 0.15, 1))
        parts.append(cube("BlasterBody", (0, 0.95, -0.26), (0.22, 0.5, 0.2), body))
        parts.append(cyl("BlasterMuzzle", (0, 1.28, -0.26), 0.08, 0.22, tip, (math.radians(90), 0, 0)))
    elif style == "emitter":  # T5: flush glowing sparkle emitter
        em = mat("Emitter", (0.12, 0.12, 0.14, 1)); glow = mat("EmitGlow", (0.40, 0.80, 1.0, 1))
        parts.append(cyl("Emitter", (0, 1.05, -0.18), 0.14, 0.25, em, (math.radians(90), 0, 0)))
        parts.append(sphere("EmitCore", (0, 1.28, -0.18), (0.1, 0.1, 0.1), glow, 1))
    elif style == "enemy":    # enemies: stubby menacing pod-blaster w/ red muzzle (still a toy)
        body = mat("EBlaster", (0.16, 0.16, 0.19, 1)); tip = mat("ETip", (0.90, 0.20, 0.15, 1))
        parts.append(cube("EBlasterBody", (0, 1.02, -0.02), (0.28, 0.55, 0.26), body))
        parts.append(cyl("EBlasterMuzzle", (0, 1.42, -0.02), 0.11, 0.30, tip, (math.radians(90), 0, 0)))

# ── PARK PROPS (children's-park environment; replaces greybox primitives in ParkMapGenerator) ──
# Built Z-up at real metre scale; ParkMapGenerator instantiates one per scatter point, sits the
# base on the ground, and adds a MeshCollider. Colours match the old primitive palette.
def build_park_tree():
    bark = mat("Bark", (0.42, 0.27, 0.14, 1)); leaf = mat("Leaf", (0.22, 0.68, 0.27, 1))
    parts = [cyl("Trunk", (0, 0, 3.0), 0.55, 6.0, bark)]
    parts.append(sphere("Leaves",  (0, 0, 6.6), (4.2, 4.2, 4.0), leaf, 2))
    parts.append(sphere("Leaves2", (1.5, 0.6, 5.3), (2.3, 2.3, 2.3), leaf, 2))
    return join(parts, "Park_Tree")

def build_park_slide():
    # Slide DESCENDS from the raised deck (back, -Y, high) down to the ground (front, +Y). Ladder at the back.
    blue = mat("PSlideBlue", (0.25, 0.5, 0.95, 1)); metal = mat("PMetal", (0.7, 0.72, 0.75, 1))
    yellow = mat("PYellow", (1, 0.82, 0.2, 1)); rail = mat("PRail", (0.95, 0.35, 0.35, 1))
    ang = math.radians(-25)   # negative → the -Y (deck) end is HIGH, +Y end low
    parts = [cube("Platform", (0, -2.6, 3.0), (3.0, 2.0, 0.4), blue)]         # raised deck
    for i, (px, py) in enumerate([(-1.2, -3.3), (1.2, -3.3), (-1.2, -1.9), (1.2, -1.9)]):
        parts.append(cube("Post%d" % i, (px, py, 1.5), (0.28, 0.28, 3.0), metal))
    # ladder at the very back
    parts.append(cube("RailL", (-0.8, -3.8, 1.5), (0.16, 0.16, 3.0), rail))
    parts.append(cube("RailR", ( 0.8, -3.8, 1.5), (0.16, 0.16, 3.0), rail))
    for i, rz in enumerate((0.7, 1.4, 2.1, 2.8)):
        parts.append(cube("Rung%d" % i, (0, -3.8, rz), (1.6, 0.12, 0.12), rail))
    # slide chute + side kerbs, connected to the deck edge and sloping down to the ground
    s = cube("Chute", (0, 0.6, 1.55), (1.8, 6.4, 0.18), yellow); s.rotation_euler = (ang, 0, 0); parts.append(s)
    kL = cube("KerbL", (-0.92, 0.6, 1.75), (0.14, 6.4, 0.42), yellow); kL.rotation_euler = (ang, 0, 0); parts.append(kL)
    kR = cube("KerbR", ( 0.92, 0.6, 1.75), (0.14, 6.4, 0.42), yellow); kR.rotation_euler = (ang, 0, 0); parts.append(kR)
    return join(parts, "Park_Slide")

def build_park_swing():
    # Indian 'jhoola' — colourful A-frame pipe swing with two FACING bench seats that rock together.
    pink = mat("JPink", (0.86, 0.22, 0.55, 1)); green = mat("JGreen", (0.20, 0.65, 0.35, 1)); yellow = mat("JYellow", (1.0, 0.80, 0.15, 1))
    blue = mat("JBlue", (0.20, 0.55, 0.90, 1)); red = mat("JRed", (0.90, 0.25, 0.25, 1)); steel = mat("JSteel", (0.70, 0.72, 0.75, 1))
    parts = []
    # A-frame at each END: two legs meet at an apex; the top bar runs between the two apexes.
    for sx in (-1, 1):
        apex = (2.2 * sx, 0.0, 4.5)
        parts.append(seg_between("LegF%d" % sx, apex, (2.2 * sx, -1.6, 0.0), 0.12, pink))
        parts.append(seg_between("LegB%d" % sx, apex, (2.2 * sx,  1.6, 0.0), 0.12, green))
    parts.append(cyl("TopBar", (0, 0, 4.5), 0.12, 4.4, yellow, (0, math.radians(90), 0)))   # spans the two apexes
    # 4 hangers straight down from the bar to the bench corners
    for hx in (-1.0, 1.0):
        for hy in (-0.9, 0.9):
            parts.append(seg_between("Hang", (hx, hy, 4.4), (hx, hy, 1.35), 0.045, steel))
    # swinging bench: floor + two facing seat/back sets
    parts.append(cube("Floor", (0, 0, 1.15), (2.6, 2.0, 0.16), green))
    parts.append(cube("SeatA", (0, -0.55, 1.5), (2.5, 0.5, 0.14), blue)); parts.append(cube("BackA", (0, -0.9, 1.82), (2.5, 0.14, 0.7), blue))
    parts.append(cube("SeatB", (0,  0.55, 1.5), (2.5, 0.5, 0.14), red));  parts.append(cube("BackB", (0,  0.9, 1.82), (2.5, 0.14, 0.7), red))
    return join(parts, "Park_Swing")

def build_park_playset():
    """Hero combined play structure (the big colourful one): two roofed towers + bridge, a straight
       slide, a spiral slide, and monkey bars. Built Z-up; large (~9m wide, decks at 4m)."""
    post = mat("PSPost", (0.20, 0.35, 0.70, 1)); deck = mat("PSDeck", (0.55, 0.57, 0.60, 1))
    rail = mat("PSRail", (0.20, 0.62, 0.45, 1)); roofA = mat("PSRoofA", (1.0, 0.82, 0.18, 1))
    roofB = mat("PSRoofB", (0.20, 0.55, 0.90, 1)); slideG = mat("PSlideG", (0.22, 0.68, 0.35, 1)); slideR = mat("PSlideR", (0.90, 0.24, 0.24, 1))
    parts = []
    decks = [(-2.4, 0.0), (2.4, 0.0)]
    for di, (dx, dy) in enumerate(decks):
        parts.append(cube("Deck%d" % di, (dx, dy, 4.0), (3.0, 3.0, 0.3), deck))
        for cx in (-1.3, 1.3):
            for cy in (-1.3, 1.3):
                parts.append(cube("Post%d_%d_%d" % (di, cx, cy), (dx + cx, dy + cy, 2.0), (0.18, 0.18, 4.0), post))  # legs
                parts.append(cube("Upr%d_%d_%d" % (di, cx, cy), (dx + cx, dy + cy, 5.2), (0.14, 0.14, 2.4), post))   # roof uprights
        # perimeter railings (leave inner side open toward the bridge)
        for (rx, ry, sx, sy) in [(0, 1.3, 3.0, 0.12), (0, -1.3, 3.0, 0.12)]:
            parts.append(cube("Rail%d_%d" % (di, ry), (dx + rx, dy + ry, 4.7), (sx, sy, 0.9), rail))
    # bridge between the two decks
    parts.append(cube("Bridge", (0, 0, 4.0), (1.9, 2.4, 0.28), deck))
    for by in (-1.0, 1.0):
        parts.append(cube("BridgeRail%d" % int(by * 10), (0, by, 4.7), (1.9, 0.1, 0.9), rail))
    # ROOFS: left tower = a tidy GABLE roof (two slanted panels + ridge), right tower = pyramid
    # gable: inner edges HIGH (meet at ridge x=-2.4), outer edges low (eaves rest on the posts)
    gL = cube("RoofGL", (-3.15, 0, 6.0), (1.9, 3.0, 0.12), roofA); gL.rotation_euler = (0, math.radians(-34), 0); parts.append(gL)
    gR = cube("RoofGR", (-1.65, 0, 6.0), (1.9, 3.0, 0.12), roofA); gR.rotation_euler = (0, math.radians(34), 0); parts.append(gR)
    parts.append(cube("Ridge", (-2.4, 0, 6.5), (0.16, 3.0, 0.16), roofB))                        # ridge cap
    parts.append(cone("RoofPyr", (2.4, 0, 6.6), 2.3, 0.0, 1.9, roofB))                           # pyramid roof, seated on the posts
    # STRAIGHT SLIDE (green) off the left deck front (-Y): HIGH end meets the deck edge, descends to ground
    ss = cube("SlideChute", (-2.4, -3.75, 2.1), (1.7, 5.9, 0.2), slideG); ss.rotation_euler = (math.radians(38), 0, 0); parts.append(ss)
    for kx in (-0.85, 0.85):
        k = cube("SlideKerb", (-2.4 + kx, -3.75, 2.35), (0.14, 5.9, 0.42), slideG); k.rotation_euler = (math.radians(38), 0, 0); parts.append(k)
    # SPIRAL SLIDE (red) off the right deck — a clean fat helical TUBE winding down a central pole
    parts.append(cyl("SpiralPole", (2.4, 2.0, 2.05), 0.16, 4.1, slideR))
    spts = []
    for k in range(29):
        frac = k / 28.0
        ang = math.radians(30 + 400 * frac)     # ~1.1 turns
        spts.append((2.4 + math.cos(ang) * 1.55, 2.0 + math.sin(ang) * 1.55, 3.85 - frac * 3.5))
    for k in range(28):
        parts.append(seg_between("Spiral%d" % k, spts[k], spts[k + 1], 0.42, slideR))
    # MONKEY BARS off the right deck (+X), horizontal ladder to two far posts
    for py in (-0.8, 0.8):
        parts.append(cube("MBRail%d" % int(py * 10), (5.6, py, 3.9), (3.2, 0.12, 0.12), post))
    for i in range(5):
        mx = 4.4 + i * 0.6
        parts.append(cube("MBRung%d" % i, (mx, 0, 3.9), (0.12, 1.7, 0.12), roofA))
    for fy in (-0.8, 0.8):
        parts.append(cube("MBPost%d" % int(fy * 10), (7.0, fy, 1.95), (0.16, 0.16, 3.9), post))
    return join(parts, "Park_Playset")

def build_park_merry():
    """Merry-go-round: segmented colourful spinning disk + centre hub + yellow grab bars."""
    hub = mat("MHub", (0.75, 0.55, 0.20, 1)); bar = mat("MBar", (1.0, 0.82, 0.15, 1))
    segcols = [mat("MSeg%d" % i, c) for i, c in enumerate([(0.25, 0.35, 0.75, 1), (0.85, 0.25, 0.25, 1), (0.22, 0.62, 0.38, 1), (0.55, 0.30, 0.62, 1)])]
    parts = [cyl("Disk", (0, 0, 0.35), 3.0, 0.25, segcols[0])]
    # pie-wedge colour patches on top (thin cubes rotated around)
    for i in range(4):
        w = cube("Wedge%d" % i, (0, 0, 0.5), (5.6, 1.4, 0.06), segcols[i % len(segcols)]); w.rotation_euler = (0, 0, math.radians(i * 45)); parts.append(w)
    parts.append(cyl("Hub", (0, 0, 0.7), 0.35, 0.8, hub))
    # clean inverted-U grab handles around the rim (two vertical posts + a horizontal top, tangent to the rim)
    for i in range(6):
        a = math.radians(i * 60); bx = math.cos(a) * 2.2; by = math.sin(a) * 2.2
        tx, ty = -math.sin(a), math.cos(a)                       # tangent direction
        for s in (-0.35, 0.35):
            parts.append(cyl("BarPost", (bx + tx * s, by + ty * s, 1.15), 0.06, 1.5, bar))   # vertical posts z=0.4..1.9
        parts.append(cyl("BarTop", (bx, by, 1.9), 0.06, 0.82, bar, (math.radians(90), 0, a)))  # top bar along tangent
    return join(parts, "Park_Merry")

def build_park_dome():
    """Dome climber: multi-colour geodesic HEMISPHERE. Meridian ribs are semicircle arches (z>=0 only, so
       it's a true dome that sits on the ground) + horizontal rings following the dome profile."""
    cols = [mat("DomeR", (0.85, 0.25, 0.25, 1)), mat("DomeG", (0.22, 0.62, 0.38, 1)), mat("DomeB", (0.25, 0.45, 0.85, 1)), mat("DomeY", (1.0, 0.82, 0.18, 1))]
    R = 5.0; parts = []
    # meridian ribs: each is a semicircle over the top (ground → apex → ground on the opposite side)
    for m in range(6):
        ma = math.radians(m * 30); col = cols[m % len(cols)]
        pts = []
        for k in range(13):
            phi = math.radians(k * 15)              # 0..180
            hr = math.cos(phi) * R; zz = math.sin(phi) * R
            pts.append((hr * math.cos(ma), hr * math.sin(ma), zz))
        for k in range(12):
            parts.append(seg_between("Rib%d_%d" % (m, k), pts[k], pts[k + 1], 0.1, col))
    # horizontal rings following the dome profile
    for i, (zz, rr) in enumerate([(0.4, 4.9), (1.8, 4.6), (3.2, 3.7), (4.3, 2.3), (4.9, 1.0)]):
        parts.append(torus("HRing%d" % i, (0, 0, zz), rr, 0.1, cols[i % len(cols)]))
    parts.append(cone("Cap", (0, 0, 5.05), 0.5, 0.0, 0.4, mat("DomeCap", (0.12, 0.12, 0.16, 1))))
    return join(parts, "Park_Dome")

def build_park_tyreswing():
    """Classic tyre swing: top beam, three chains, a black tyre lying flat."""
    beam = mat("TSBeam", (0.60, 0.62, 0.66, 1)); chain = mat("TSChain", (0.85, 0.20, 0.20, 1)); rubber = mat("TSTyre", (0.10, 0.10, 0.12, 1))
    parts = [cube("Beam", (0, 0, 4.2), (3.4, 0.25, 0.25), beam)]
    parts.append(cyl("BeamPostL", (-1.6, 0, 2.1), 0.12, 4.2, beam))
    parts.append(cyl("BeamPostR", ( 1.6, 0, 2.1), 0.12, 4.2, beam))
    top = (0, 0, 3.95)                        # chains converge just under the beam...
    for a in (90, 210, 330):
        ar = math.radians(a)
        rim = (math.cos(ar) * 0.75, math.sin(ar) * 0.75, 1.45)   # ...and fan out to 3 points on the tyre rim
        parts.append(seg_between("Chain%d" % a, top, rim, 0.04, chain))
    parts.append(torus("Tyre", (0, 0, 1.1), 0.9, 0.35, rubber))  # default torus = lies flat (hole up)
    return join(parts, "Park_Tyreswing")

def build_park_rockwall():
    """Rock-climbing wall: grey panel leaning back on A-frame legs, scattered colourful holds."""
    panel = mat("RWPanel", (0.42, 0.43, 0.46, 1)); frame = mat("RWFrame", (0.55, 0.57, 0.60, 1))
    holds = [mat("RWHold%d" % i, c) for i, c in enumerate([(0.85, 0.25, 0.25, 1), (0.25, 0.50, 0.90, 1), (1.0, 0.82, 0.20, 1), (0.25, 0.65, 0.35, 1)])]
    pnl = cube("Panel", (0, 0, 2.6), (2.6, 0.25, 5.2), panel); pnl.rotation_euler = (math.radians(-10), 0, 0)
    parts = [pnl]
    for sx in (-1, 1):
        br = cube("Brace%d" % sx, (0.9 * sx, 0.9, 2.4), (0.16, 0.16, 4.6), frame); br.rotation_euler = (math.radians(28), 0, 0); parts.append(br)  # angled A-frame support
        parts.append(cube("Foot%d" % sx, (0.9 * sx, 1.9, 0.12), (0.2, 1.2, 0.24), frame))
    for i, (hx, hz) in enumerate([(-0.7, 3.2), (0.6, 4.2), (-0.3, 4.8), (0.9, 2.6), (-0.9, 1.8), (0.2, 3.6), (0.7, 5.4), (-0.5, 2.4), (0.1, 5.0), (-0.8, 4.4)]):
        parts.append(sphere("Hold%d" % i, (hx, -0.22, hz), (0.16, 0.12, 0.16), holds[i % len(holds)], 1))
    return join(parts, "Park_Rockwall")

def build_park_tyrewall():
    """Tyre-climbing grid: dark steel frame holding a 5×4 grid of colour-banded tyres (vertical rings)."""
    frame = mat("TWFrame", (0.15, 0.16, 0.20, 1))
    cols = [mat("TWT%d" % i, c) for i, c in enumerate([(0.90, 0.80, 0.20, 1), (0.12, 0.12, 0.14, 1), (0.85, 0.25, 0.25, 1), (0.25, 0.60, 0.35, 1)])]
    parts = []
    for sx in (-1, 1):
        parts.append(cube("Post%d" % sx, (3.0 * sx, 0, 2.8), (0.2, 0.2, 5.6), frame))
    parts.append(cube("TopBar", (0, 0, 5.4), (6.2, 0.2, 0.2), frame))
    for r in range(4):
        for c in range(5):
            parts.append(torus("Tyre_%d_%d" % (r, c), (-2.4 + c * 1.2, 0, 1.2 + r * 1.15), 0.5, 0.2, cols[r % len(cols)], (math.radians(90), 0, 0)))
    return join(parts, "Park_Tyrewall")

def build_park_trampoline():
    """Round trampoline: padded blue rim, dark jump mat, splayed legs."""
    rim = mat("TrRim", (0.20, 0.45, 0.85, 1)); jm = mat("TrMat", (0.10, 0.11, 0.14, 1)); leg = mat("TrLeg", (0.30, 0.31, 0.34, 1))
    parts = [cyl("Mat", (0, 0, 1.0), 1.9, 0.12, jm), torus("Rim", (0, 0, 1.05), 2.0, 0.16, rim)]
    for i in range(6):
        a = math.radians(i * 60); parts.append(cyl("Leg%d" % i, (math.cos(a) * 1.7, math.sin(a) * 1.7, 0.5), 0.08, 1.0, leg))
    return join(parts, "Park_Trampoline")

def build_park_bench():
    """Green slatted park bench."""
    green = mat("BenchG", (0.20, 0.55, 0.32, 1))
    parts = []
    for sy in (-0.25, 0.05, 0.35):
        parts.append(cube("Seat%d" % int(sy * 100), (0, sy, 1.0), (3.0, 0.22, 0.1), green))
    for bz in (1.4, 1.7, 2.0):
        parts.append(cube("Back%d" % int(bz * 10), (0, 0.5, bz), (3.0, 0.1, 0.22), green))
    for sx in (-1, 1):
        parts.append(cube("Leg%d" % sx, (1.35 * sx, 0.4, 0.5), (0.16, 0.9, 1.0), green))
    return join(parts, "Park_Bench")

def build_park_animalmerry():
    """Merry-go-round with simple colourful animal seats (body + neck + head + grip)."""
    disk = mat("AMDisk", (0.85, 0.85, 0.88, 1)); hub = mat("AMHub", (0.30, 0.55, 0.85, 1))
    cols = [mat("AMh%d" % i, c) for i, c in enumerate([(0.90, 0.80, 0.20, 1), (0.85, 0.25, 0.25, 1), (0.25, 0.65, 0.38, 1), (0.25, 0.50, 0.85, 1)])]
    parts = [cyl("Disk", (0, 0, 0.35), 3.0, 0.25, disk), cyl("Hub", (0, 0, 1.0), 0.3, 1.4, hub)]
    for i in range(4):
        a = math.radians(i * 90); bx = math.cos(a) * 1.9; by = math.sin(a) * 1.9; col = cols[i % len(cols)]
        fx, fy = -math.sin(a), math.cos(a)          # facing = tangent (direction of spin)
        rx, ry = math.cos(a), math.sin(a)           # sideways = radial
        bd = cube("Body%d" % i, (bx, by, 1.15), (0.45, 1.3, 0.55), col); bd.rotation_euler = (0, 0, a); parts.append(bd)   # elongated along facing
        for lf in (-0.42, 0.42):                    # 4 legs
            for ls in (-0.2, 0.2):
                parts.append(cube("Leg", (bx + fx * lf + rx * ls, by + fy * lf + ry * ls, 0.82), (0.14, 0.14, 0.85), col))
        nx, ny = bx + fx * 0.55, by + fy * 0.55     # neck + head at the front
        parts.append(cube("Neck%d" % i, (nx, ny, 1.62), (0.26, 0.3, 0.72), col))
        hd = cube("Head%d" % i, (bx + fx * 0.75, by + fy * 0.75, 2.02), (0.3, 0.52, 0.32), col); hd.rotation_euler = (0, 0, a); parts.append(hd)  # snout points forward
        for es in (-0.1, 0.1):                      # ears
            parts.append(cube("Ear", (bx + fx * 0.7 + rx * es, by + fy * 0.7 + ry * es, 2.3), (0.09, 0.09, 0.16), col))
        parts.append(cube("Tail%d" % i, (bx - fx * 0.7, by - fy * 0.7, 1.35), (0.1, 0.1, 0.5), col))   # tail at the back
        parts.append(cyl("Grip%d" % i, (bx, by, 1.95), 0.05, 0.7, hub))                                # grip pole
    return join(parts, "Park_Animalmerry")

def build_park_gym():
    orange = mat("PGymO", (1, 0.55, 0.15, 1)); blue = mat("PGymB", (0.25, 0.5, 0.95, 1)); s = 4.0
    parts = []
    for (cx, cy) in [(-s, -s), (s, -s), (-s, s), (s, s)]:
        parts.append(cube("Post_%d_%d" % (cx, cy), (cx, cy, 2.5), (0.3, 0.3, 5.0), orange))
    parts.append(cube("FrameF", (0, -s, 5.0), (s * 2, 0.3, 0.3), orange))
    parts.append(cube("FrameB", (0,  s, 5.0), (s * 2, 0.3, 0.3), orange))
    parts.append(cube("FrameL", (-s, 0, 5.0), (0.3, s * 2, 0.3), orange))
    parts.append(cube("FrameR", ( s, 0, 5.0), (0.3, s * 2, 0.3), orange))
    parts.append(cube("MidPlat", (0, 0, 2.6), (s * 2, 2.0, 0.25), blue))
    return join(parts, "Park_Gym")

def build_park_sandbox():
    frame = mat("PSand", (1, 0.82, 0.2, 1)); fill = mat("PSandFill", (0.85, 0.75, 0.5, 1)); s = 5.0; wall = 0.9
    parts = [cube("W1", (0,  s, wall * 0.5), (s * 2, 0.5, wall), frame)]
    parts.append(cube("W2", (0, -s, wall * 0.5), (s * 2, 0.5, wall), frame))
    parts.append(cube("W3", ( s, 0, wall * 0.5), (0.5, s * 2, wall), frame))
    parts.append(cube("W4", (-s, 0, wall * 0.5), (0.5, s * 2, wall), frame))
    parts.append(cube("Fill", (0, 0, 0.15), (s * 2, s * 2, 0.3), fill))
    return join(parts, "Park_Sandbox")

def build_park_seesaw():
    red = mat("PSeeR", (0.9, 0.25, 0.25, 1)); green = mat("PSeeG", (0.3, 0.75, 0.35, 1))
    parts = [cube("Fulcrum", (0, 0, 0.6), (0.8, 0.8, 1.2), red)]
    plank = cube("Plank", (0, 0, 1.2), (0.6, 6.0, 0.2), green); plank.rotation_euler = (math.radians(9), 0, 0)
    parts.append(plank)
    return join(parts, "Park_Seesaw")

def build_one(fn, name):
    reset()
    obj = fn()
    export(obj, name)
    render(name, obj)
    log("built " + name)

# PLAYER = realistic quadcopter ladder (grounded, believable prop/body ratio)
build_one(build_quad_player, "drone_player_t0")   # DIY plywood X-quad
build_one(build_quad_t2,     "drone_player_t2")   # consumer, integrated guards
build_one(build_quad_t3,     "drone_player_t3")   # prosumer, folding + gimbal
build_one(build_quad_t5,     "drone_player_t5")   # ascendant, ducted + sensors
# World objects
build_one(build_core,        "core")
build_one(build_outpost,     "outpost")
# ENEMIES + BOSS
build_one(build_boss_octa,   "boss_octa")      # final purple octacopter boss
build_one(build_tier0_junk,  "enemy_binbot")
build_one(build_player_drone, "enemy_brasspod")
# PARK PROPS
build_one(build_park_tree,    "park_tree")
build_one(build_park_slide,   "park_slide")
build_one(build_park_swing,   "park_swing")
build_one(build_park_gym,     "park_gym")
build_one(build_park_sandbox, "park_sandbox")
build_one(build_park_seesaw,  "park_seesaw")
build_one(build_park_playset, "park_playset")   # hero combined structure
build_one(build_park_merry,   "park_merry")     # merry-go-round
build_one(build_park_dome,    "park_dome")      # dome climber (replaces box gym)
build_one(build_park_tyreswing, "park_tyreswing")
build_one(build_park_rockwall,  "park_rockwall")
build_one(build_park_tyrewall,  "park_tyrewall")
build_one(build_park_trampoline, "park_trampoline")
build_one(build_park_bench,     "park_bench")
build_one(build_park_animalmerry, "park_animalmerry")
log("DONE")
print("BUILD_OK")
