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

# Art-Direction palette (see Udaan-Brain/Art-Direction.md)
BRASS  = (0.79, 0.63, 0.15, 1)
BRONZE = (0.55, 0.42, 0.25, 1)
CANVAS = (0.94, 0.89, 0.78, 1)
BLUE   = (0.30, 0.62, 1.00, 1)   # friendly accent / gameplay-readable
WOOD   = (0.61, 0.42, 0.24, 1)
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
    fbx = os.path.join(ASSETS, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True,
                             apply_unit_scale=True, object_types={'MESH'},
                             mesh_smooth_type='FACE')
    log("exported " + fbx)

def render(name):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'   # show each material's colour
    scene.render.resolution_x = 640
    scene.render.resolution_y = 480
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")
    # 3/4 camera looking at origin
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (6.5, -6.5, 4.5)
    d = mathutils.Vector((0, 0, 0.3)) - cam.location
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
        parts.append(cube(f"Blade{sx}{sy}a", (mx, my, 0.29), (1.6, 0.16, 0.02), prop))  # big 2-blade prop
        parts.append(cube(f"Blade{sx}{sy}b", (mx, my, 0.29), (0.16, 1.6, 0.02), prop))
    # simple landing legs
    for (sx, sy) in [(1, 1), (-1, -1)]:
        parts.append(cyl(f"Leg{sx}{sy}", (0.5 * sx, 0.5 * sy, -0.28), 0.04, 0.6, dark))
    _blaster(parts, "foam")
    return join(parts, "Drone_Player_T0")

def torus(name, loc, major, minor, material):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=loc)
    o = bpy.context.active_object; o.name = name
    o.data.materials.append(material)
    return o

def _xframe(parts, dark, span=2.9, thick=0.14, zh=0.06):
    a1 = cube("ArmA", (0, 0, 0), (thick, span, zh), dark); a1.rotation_euler = (0, 0, math.radians(45))
    a2 = cube("ArmB", (0, 0, 0), (thick, span, zh), dark); a2.rotation_euler = (0, 0, math.radians(-45))
    parts += [a1, a2]

def _props4(parts, d, motor_mat, prop_mat, prad, z=0.26, guard_mat=None, guard_r=None):
    for sx, sy in [(1, 1), (1, -1), (-1, 1), (-1, -1)]:
        mx, my = d * sx, d * sy
        parts.append(cyl(f"M{sx}_{sy}", (mx, my, z - 0.12), 0.12, 0.16, motor_mat))
        parts.append(cube(f"B{sx}_{sy}a", (mx, my, z), (prad * 2, 0.13, 0.02), prop_mat))
        parts.append(cube(f"B{sx}_{sy}b", (mx, my, z), (0.13, prad * 2, 0.02), prop_mat))
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

def build_one(fn, name):
    reset()
    obj = fn()
    export(obj, name)
    render(name)
    log("built " + name)

# PLAYER = realistic quadcopter ladder (grounded, believable prop/body ratio)
build_one(build_quad_player, "drone_player_t0")   # DIY plywood X-quad
build_one(build_quad_t2,     "drone_player_t2")   # consumer, integrated guards
build_one(build_quad_t3,     "drone_player_t3")   # prosumer, folding + gimbal
build_one(build_quad_t5,     "drone_player_t5")   # ascendant, ducted + sensors
# World objects
build_one(build_core,        "core")
build_one(build_outpost,     "outpost")
# ENEMIES = the earlier fantasy builds get reused as hostiles (creative side)
build_one(build_tier0_junk,  "enemy_binbot")
build_one(build_player_drone, "enemy_brasspod")
log("DONE")
print("BUILD_OK")
