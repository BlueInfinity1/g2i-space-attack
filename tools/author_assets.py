"""Author Unity scene/prefab/config YAML directly. Run before importing the project.

This is an offline text authoring tool, not a runtime scene bootstrap. Assets remain
normal editable Unity files. Do not rerun over Inspector edits without reviewing them.
"""
from pathlib import Path
import hashlib
import math
import random
import json
import argparse

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--output", default=".", help="Workspace-relative staging output; review before copying assets into the project.")
OUTPUT = (ROOT / parser.parse_args().output).resolve()
assert OUTPUT == ROOT or ROOT in OUTPUT.parents, "Output must remain inside this workspace"
ASSETS = ROOT / "Assets/SpaceAttack"
UGUI = Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui")

def guid(path):
    return hashlib.md5(("space-attack/" + str(path).replace("\\", "/")).encode()).hexdigest()

def write(path, content):
    target = OUTPUT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content, encoding="utf-8")

def ref(path, fileid=11400000):
    asset_type = 3 if str(path).endswith((".shader", ".prefab")) else 2
    return "{fileID: %s, guid: %s, type: %s}" % (fileid, guid(path), asset_type)

def script(name):
    matches = list((ASSETS / "Scripts").rglob(name + ".cs"))
    assert len(matches) == 1, name
    return "{fileID: 11500000, guid: %s, type: 3}" % guid(matches[0].relative_to(ROOT).as_posix())

def ugui(name):
    meta = next(UGUI.rglob(name + ".cs.meta")).read_text()
    value = next(line.split(": ", 1)[1] for line in meta.splitlines() if line.startswith("guid:"))
    return "{fileID: 11500000, guid: %s, type: 3}" % value

def color(c):
    return "{r: %s, g: %s, b: %s, a: %s}" % tuple(c)

HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"

class Document:
    def __init__(self):
        self.objects = []
        self.next_id = 1000
        self.nodes = {}

    def alloc(self):
        self.next_id += 1
        return self.next_id

    def component(self, node, cls, typename, text):
        fid = self.alloc()
        node["components"].append(fid)
        self.objects.append((cls, fid, typename, f"  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {node['id']}}}\n" + text))
        return fid

    def mono(self, node, name, text="", package=False):
        return self.component(node, 114, "MonoBehaviour", "  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: " + (ugui(name) if package else script(name)) + "\n  m_Name: \n  m_EditorClassIdentifier: \n" + text)

    def node(self, name, parent=None, pos=(0,0,0), scale=(1,1,1), angle=0, ui=None, active=1, layer=0):
        n = dict(id=self.alloc(), transform=self.alloc(), name=name, parent=parent, children=[], components=[], pos=pos, scale=scale, angle=angle, ui=ui, active=active, layer=layer)
        n["components"].append(n["transform"])
        if parent: parent["children"].append(n["transform"])
        self.nodes[n["id"]] = n
        return n

    def cube(self, name, parent, pos, scale, material, angle=0):
        n = self.node(name, parent, pos, scale, angle)
        self.component(n, 33, "MeshFilter", "  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}\n")
        r = self.component(n, 23, "MeshRenderer", "  m_Enabled: 1\n  m_CastShadows: 0\n  m_ReceiveShadows: 0\n  m_DynamicOccludee: 0\n  m_MotionVectors: 0\n  m_LightProbeUsage: 0\n  m_ReflectionProbeUsage: 0\n  m_RenderingLayerMask: 1\n  m_Materials:\n  - " + ref(f"Assets/SpaceAttack/Art/{material}.mat",2100000) + "\n  m_SortingLayerID: 0\n  m_SortingOrder: 0\n")
        return n,r

    def physics(self, n, size):
        rb = self.component(n, 50, "Rigidbody2D", "  serializedVersion: 5\n  m_BodyType: 0\n  m_Simulated: 1\n  m_UseFullKinematicContacts: 0\n  m_UseAutoMass: 0\n  m_Mass: 1\n  m_LinearDamping: 0\n  m_AngularDamping: 0\n  m_GravityScale: 0\n  m_Interpolation: 1\n  m_CollisionDetection: 1\n  m_Constraints: 4\n")
        self.component(n, 61, "BoxCollider2D", "  m_Enabled: 1\n  serializedVersion: 3\n  m_Density: 1\n  m_Material: {fileID: 0}\n  m_IsTrigger: 1\n  m_UsedByEffector: 0\n  m_Offset: {x: 0, y: 0}\n  m_Size: {x: %s, y: %s}\n  m_EdgeRadius: 0\n" % size)
        return rb

    def render(self, scene=False):
        output = HEADER
        for n in self.nodes.values():
            output += f"--- !u!1 &{n['id']}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n"
            output += "".join(f"  - component: {{fileID: {i}}}\n" for i in n["components"])
            output += f"  m_Layer: {n['layer']}\n  m_Name: {n['name']}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: {n['active']}\n"
            cls,typ = (224,"RectTransform") if n["ui"] is not None else (4,"Transform")
            output += f"--- !u!{cls} &{n['transform']}\n{typ}:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['id']}}}\n"
            rad = n["angle"] * math.pi / 360
            output += "  m_LocalRotation: {x: 0, y: 0, z: %s, w: %s}\n" % (math.sin(rad),math.cos(rad))
            output += "  m_LocalPosition: {x: %s, y: %s, z: %s}\n" % n["pos"]
            output += "  m_LocalScale: {x: %s, y: %s, z: %s}\n" % n["scale"]
            output += "  m_ConstrainProportionsScale: 0\n  m_Children:" + ("\n" + "".join(f"  - {{fileID: {i}}}\n" for i in n["children"]) if n["children"] else " []\n")
            output += f"  m_Father: {{fileID: {n['parent']['transform'] if n['parent'] else 0}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {n['angle']}}}\n"
            if n["ui"] is not None:
                u = n["ui"]
                amin,amax=u.get("anchors",((0.5,0.5),(0.5,0.5)))
                output += "  m_AnchorMin: {x: %s, y: %s}\n  m_AnchorMax: {x: %s, y: %s}\n" % (*amin,*amax)
                output += "  m_AnchoredPosition: {x: %s, y: %s}\n  m_SizeDelta: {x: %s, y: %s}\n  m_Pivot: {x: 0.5, y: 0.5}\n" % (*u.get("xy",(0,0)),*u.get("size",(100,30)))
        for cls,fid,typ,body in self.objects:
            output += f"--- !u!{cls} &{fid}\n{typ}:\n{body}"
        if scene:
            output += "--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n"
            output += "".join(f"  - {{fileID: {n['transform']}}}\n" for n in self.nodes.values() if n["parent"] is None)
        return output

def config(name, cls, fields):
    write(f"Assets/SpaceAttack/Settings/{name}.asset", HEADER + f"--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 0}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {script(cls)}\n  m_Name: {name}\n  m_EditorClassIdentifier: \n" + fields)

PALETTE = {"Cyan":(0.15,0.95,1,1), "Red":(1,0.22,0.32,1), "Green":(0.35,1,0.5,1), "Yellow":(1,0.82,0.26,1), "Dark":(0.015,0.03,0.065,1), "White":(1,1,1,1), "Line":(0.07,0.17,0.24,1), "Star":(0.16,0.29,0.4,1)}
for name,c in PALETTE.items():
    write(f"Assets/SpaceAttack/Art/{name}.mat", HEADER + f"--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_Name: {name}\n  m_Shader: {ref('Assets/SpaceAttack/Art/FlatColor.shader',4800000)}\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap: {{}}\n  disabledShaderPasses: []\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats: []\n    m_Colors:\n    - _Color: {color(c)}\n")

config("PlayerWeapon", "WeaponConfig", "  fireIntervalSeconds: 0.18\n  projectileSpeed: 24\n  projectileDamage: 1\n  projectileLifetimeSeconds: 1.3\n")
config("EnemyWeapon", "WeaponConfig", "  fireIntervalSeconds: 6\n  projectileSpeed: 4.8\n  projectileDamage: 1\n  projectileLifetimeSeconds: 4\n")
config("Player", "PlayerConfig", "  moveSpeed: 10\n  maxHealth: 3\n  invulnerabilitySeconds: 1.2\n  weapon: " + ref("Assets/SpaceAttack/Settings/PlayerWeapon.asset") + "\n")
for kind,name,score in [(0,"Red",30),(1,"Green",40),(2,"Yellow",60)]:
    config(name+"Enemy","EnemyConfig",f"  kind: {kind}\n  attackSpeed: 3.6\n  attackAngleFromVerticalDegrees: 35\n  directionChangeDelaySeconds: 0.8\n  directionCheckIntervalSeconds: 0.4\n  directionChangeInitialChance: 0.06\n  directionChanceGrowthPerSecond: 0.06\n  directionChangeMaximumChance: 0.4\n  weapon: {ref('Assets/SpaceAttack/Settings/EnemyWeapon.asset')}\n  contactDamage: 1\n  formationScore: {score}\n  color: {color(PALETTE[name])}\n")
config("Waves","WaveConfig","  attackStartDelaySeconds: 1.6\n  attackIntervalSeconds: 0.95\n  maxConcurrentAttackers: 8\n  interWaveDelaySeconds: 1.8\n  difficultyIncreasePerWave: 0.2\n  maxDifficultyMultiplier: 3\n  formationMinOffsetX: -2.5\n  formationMaxOffsetX: 2.5\n  formationSpeed: 0.4\n")
config("Scoring","ScoringConfig","  attackMinMultiplier: 4\n  attackMaxMultiplier: 10\n  scoreRoundingStep: 10\n")
config("Feedback","FeedbackConfig","  screenShakeIntensity: 0.2\n  screenShakeDurationSeconds: 0.15\n  hitFlashIntensity: 0.25\n  hitFlashDurationSeconds: 0.1\n  screenFlashIntensity: 0\n  screenFlashDurationSeconds: 0.1\n  explosionParticleAmount: 0.6\n  backgroundMotionIntensity: 0.15\n  invulnerabilityBlinkIntensity: 1\n  invulnerabilityBlinkIntervalSeconds: 0.12\n  invulnerabilityMinimumAlpha: 0.5\n  masterVolume: 0.9\n  sfxVolume: 0.8\n")
config("Game","GameConfig","".join(f"  {field}: {ref('Assets/SpaceAttack/Settings/'+asset+'.asset')}\n" for field,asset in [("player","Player"),("redEnemy","RedEnemy"),("greenEnemy","GreenEnemy"),("yellowEnemy","YellowEnemy"),("waves","Waves"),("scoring","Scoring"),("feedback","Feedback")]))

# Reusable projectile and enemy prefabs. All visual parts use Unity's built-in cube.
p=Document()
bullet=p.node("Player Projectile",layer=10)
p.physics(bullet,(0.12,0.4))
projectile_id=p.mono(bullet,"Projectile")
p.cube("Bolt",bullet,(0,0,0),(0.1,0.45,0.08),"Cyan")
p.cube("Core",bullet,(0,0,-0.05),(0.035,0.3,0.05),"White")
write("Assets/SpaceAttack/Prefabs/Projectile.prefab",p.render())

e=Document()
enemy=e.node("Enemy",layer=9)
e.physics(enemy,(0.72,0.42))
e.mono(enemy,"ShipHealth")
parts=[]
for name,pos,scale,angle in [("Core",(0,0,0),(.4,.26,.1),0),("Left Wing",(-.27,.04,0),(.24,.14,.1),-25),("Right Wing",(.27,.04,0),(.24,.14,.1),25),("Left Foot",(-.18,-.18,0),(.13,.12,.1),-30),("Right Foot",(.18,-.18,0),(.13,.12,.1),30)]:
    _,renderer=e.cube(name,enemy,pos,scale,"Red",angle)
    parts.append(renderer)
e.cube("Left Eye",enemy,(-.09,.025,-.1),(.055,.065,.04),"Dark")
e.cube("Right Eye",enemy,(.09,.025,-.1),(.055,.065,.04),"Dark")
enemy_id=e.mono(enemy,"EnemyController","  coloredParts:\n"+"".join(f"  - {{fileID: {x}}}\n" for x in parts))
enemy_muzzle=e.node("Muzzle",enemy,(0,-.5,0))
e.mono(enemy,"Weapon",f"  projectilePrefab: {ref('Assets/SpaceAttack/Prefabs/Projectile.prefab',projectile_id)}\n  muzzle: {{fileID: {enemy_muzzle['transform']}}}\n")
write("Assets/SpaceAttack/Prefabs/Enemy.prefab",e.render())

spark_doc=Document()
spark,spark_renderer=spark_doc.cube("Spark",None,(0,0,0),(.075,.075,.05),"White")
spark_id=spark_doc.mono(spark,"EffectParticle")
write("Assets/SpaceAttack/Prefabs/Spark.prefab",spark_doc.render())

d=Document()
root=d.node("Game")
systems=d.node("Systems",root)
playfield=d.node("Playfield",root)
bounds=d.mono(playfield,"PlayfieldBounds","  halfWidth: 9\n  playerY: -6.4\n  dangerLineY: -6.1\n  escapeBottomY: -8.4\n  escapeTopY: 8.4\n  escapeSideMargin: 1\n")
slotsRoot=d.node("FormationSlots",playfield)
slots=[]
layout=[([3,5],2),(list(range(2,7)),0),(list(range(1,8)),1)]+[(list(range(9)),0)]*3
for row,(columns,kind) in enumerate(layout):
    for column in columns:
        n=d.node(f"Slot_{len(slots)+1:02}",slotsRoot,((column-4)*1.13,5.6-row*.62,0))
        slots.append(d.mono(n,"FormationSlot",f"  kind: {kind}\n"))
runtime=d.node("Runtime",root)
enemies=d.node("Enemies",runtime)
projectiles=d.node("Projectiles",runtime)
effects=d.node("Effects",runtime)
wave=d.mono(systems,"WaveDirector",f"  enemyPrefab: {ref('Assets/SpaceAttack/Prefabs/Enemy.prefab',enemy_id)}\n  enemiesRoot: {{fileID: {enemies['transform']}}}\n  projectilesRoot: {{fileID: {projectiles['transform']}}}\n  slots:\n"+"".join(f"  - {{fileID: {s}}}\n" for s in slots))

player=d.node("Player",root,(0,-6.4,0),layer=8)
d.physics(player,(.86,.46))
d.mono(player,"ShipHealth")
for name,pos,scale,angle in [("Nose",(0,.16,0),(.27,.54,.14),0),("Hull",(0,-.02,0),(.48,.34,.14),0),("Left Wing",(-.35,-.13,0),(.48,.18,.14),20),("Right Wing",(.35,-.13,0),(.48,.18,.14),-20)]:
    d.cube(name,player,pos,scale,"Cyan",angle)
d.cube("Cockpit",player,(0,.09,-.1),(.105,.17,.04),"Dark")
muzzle=d.node("Muzzle",player,(0,.6,0))
d.mono(player,"Weapon",f"  projectilePrefab: {ref('Assets/SpaceAttack/Prefabs/Projectile.prefab',projectile_id)}\n  muzzle: {{fileID: {muzzle['transform']}}}\n")
player_control=d.mono(player,"PlayerController")

camera=d.node("Main Camera",root,(0,0,-10))
d.component(camera,20,"Camera","  m_Enabled: 1\n  serializedVersion: 2\n  m_ClearFlags: 2\n  m_BackGroundColor: {r: 0.008, g: 0.016, b: 0.035, a: 1}\n  m_projectionMatrixMode: 1\n  m_GateFitMode: 2\n  m_FOVAxisMode: 0\n  m_NormalizedViewPortRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1\n  near clip plane: 0.3\n  far clip plane: 100\n  field of view: 60\n  orthographic: 1\n  orthographic size: 9\n  m_Depth: -1\n  m_CullingMask:\n    serializedVersion: 2\n    m_Bits: 4294967295\n  m_RenderingPath: -1\n  m_TargetTexture: {fileID: 0}\n  m_TargetDisplay: 0\n  m_TargetEye: 3\n  m_HDR: 0\n  m_AllowMSAA: 0\n  m_AllowDynamicResolution: 0\n  m_ForceIntoRT: 0\n  m_OcclusionCulling: 0\n")
d.component(camera,81,"AudioListener","  m_Enabled: 1\n")
d.mono(camera,"CameraFraming","  minimumHalfHeight: 9\n  minimumHalfWidth: 10.5\n")

decor=d.node("Primitive Starfield",root)
rng=random.Random(19)
for i in range(65):
    s=rng.uniform(.025,.055)
    d.cube(f"Star_{i:02}",decor,(rng.uniform(-13,13),rng.uniform(-8.5,8.5),2),(s,s,.04),"Star")
border=d.node("Playfield Border",root)
d.cube("Lower Boundary",border,(0,-7.15,1),(19.4,.045,.04),"Line")

# Canvas objects, text and buttons are all serialized, not constructed at runtime.
canvas=d.node("Canvas",root,ui={"anchors":((0,0),(1,1)),"size":(0,0)},layer=5)
d.component(canvas,223,"Canvas","  m_Enabled: 1\n  serializedVersion: 3\n  m_RenderMode: 0\n  m_Camera: {fileID: 0}\n  m_PlaneDistance: 100\n  m_PixelPerfect: 0\n  m_ReceivesEvents: 1\n  m_OverrideSorting: 0\n  m_OverridePixelPerfect: 0\n  m_SortingBucketNormalizedSize: 0\n  m_VertexColorAlwaysGammaSpace: 0\n  m_AdditionalShaderChannelsFlag: 0\n  m_SortingLayerID: 0\n  m_SortingOrder: 0\n  m_TargetDisplay: 0\n")
d.mono(canvas,"CanvasScaler","  m_UiScaleMode: 1\n  m_ReferencePixelsPerUnit: 100\n  m_ScaleFactor: 1\n  m_ReferenceResolution: {x: 1280, y: 900}\n  m_ScreenMatchMode: 0\n  m_MatchWidthOrHeight: 0.5\n  m_PhysicalUnit: 3\n  m_FallbackScreenDPI: 96\n  m_DefaultSpriteDPI: 96\n  m_DynamicPixelsPerUnit: 1\n",True)
d.mono(canvas,"GraphicRaycaster","  m_IgnoreReversedGraphics: 1\n  m_BlockingObjects: 0\n  m_BlockingMask:\n    serializedVersion: 2\n    m_Bits: 4294967295\n",True)

def rect(name,parent,x,y,w,h,anchors=None):
    return d.node(name,parent,ui={"xy":(x,y),"size":(w,h),"anchors":anchors or ((.5,.5),(.5,.5))},layer=5)

def graphic(n):
    d.component(n,222,"CanvasRenderer","  m_CullTransparentMesh: 1\n")

def image(n,c,raycast=0):
    graphic(n)
    return d.mono(n,"Image",f"  m_Material: {{fileID: 0}}\n  m_Color: {color(c)}\n  m_RaycastTarget: {raycast}\n  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: {{fileID: 0}}\n  m_Type: 0\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: 1\n",True)

def text(name,parent,caption,x,y,w,h,size=20,c=(.65,.76,.83,1),align=4,bold=False,anchors=None):
    n=rect(name,parent,x,y,w,h,anchors)
    graphic(n)
    tid=d.mono(n,"Text",f"  m_Material: {{fileID: 0}}\n  m_Color: {color(c)}\n  m_RaycastTarget: 0\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_FontData:\n    m_Font: {{fileID: 12800000, guid: 0000000000000000e000000000000000, type: 0}}\n    m_FontSize: {size}\n    m_FontStyle: {1 if bold else 0}\n    m_BestFit: 0\n    m_MinSize: 10\n    m_MaxSize: {size}\n    m_Alignment: {align}\n    m_AlignByGeometry: 0\n    m_RichText: 1\n    m_HorizontalOverflow: 0\n    m_VerticalOverflow: 0\n    m_LineSpacing: 1\n  m_Text: {json.dumps(caption,ensure_ascii=True)}\n",True)
    return tid

def button(name,parent,label,y,x=0,width=340,height=60,anchors=None,size=20):
    n=rect(name,parent,x,y,width,height,anchors)
    img=image(n,(.1,.88,.93,1),1)
    b=d.mono(n,"Button",f"  m_Navigation:\n    m_Mode: 0\n    m_WrapAround: 0\n    m_SelectOnUp: {{fileID: 0}}\n    m_SelectOnDown: {{fileID: 0}}\n    m_SelectOnLeft: {{fileID: 0}}\n    m_SelectOnRight: {{fileID: 0}}\n  m_Transition: 1\n  m_Colors:\n    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}\n    m_HighlightedColor: {{r: 0.8, g: 1, b: 1, a: 1}}\n    m_PressedColor: {{r: 0.55, g: 0.85, b: 0.9, a: 1}}\n    m_SelectedColor: {{r: 1, g: 1, b: 1, a: 1}}\n    m_DisabledColor: {{r: 0.5, g: 0.5, b: 0.5, a: 0.5}}\n    m_ColorMultiplier: 1\n    m_FadeDuration: 0.1\n  m_Interactable: 1\n  m_TargetGraphic: {{fileID: {img}}}\n  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n",True)
    text(name+" Label",n,label,0,0,width-20,height-6,size,(.015,.04,.08,1),bold=True)
    return b

top=((.5,1),(.5,1)); bottom=((.5,0),(.5,0))
text("Score Label",canvas,"SCORE",-420,-34,240,24,14,align=3,anchors=top)
score=text("Score",canvas,"000000",-420,-70,240,48,36,PALETTE["White"],3,True,top)
text("Best Label",canvas,"SESSION BEST",420,-34,240,24,14,align=5,anchors=top)
best=text("Best",canvas,"000000",420,-70,240,48,36,PALETTE["White"],5,True,top)
text("Wordmark",canvas,"SPACE ATTACK",0,-42,450,35,20,PALETTE["Cyan"],bold=True,anchors=top)
stage=text("Stage",canvas,"STAGE  01",0,-79,450,30,15,anchors=top)
image(rect("Header Rule",canvas,0,-110,1080,2,top),(.07,.2,.28,1))
status=text("Stage Announcement",canvas,"",0,-140,900,28,16,PALETTE["Cyan"],anchors=top)
health=text("Energy",canvas,"ENERGY   3 / 3",-395,66,290,28,20,PALETTE["Cyan"],3,True,bottom)
energy_track=rect("Energy Track",canvas,-395,46,290,18,bottom)
image(energy_track,PALETTE["Line"])
energy_fill=rect("Energy Fill",energy_track,0,0,0,0,((0,0),(1,1)))
image(energy_fill,PALETTE["Cyan"])
text("Controls",canvas,"A / D or ARROW KEYS - MOVE\nSPACE - FIRE",350,58,380,42,16,align=5,anchors=bottom)
settings_button=button("Settings Button",canvas,"SETTINGS / ESC",52,width=180,height=36,anchors=bottom,size=15)
comfort=text("Comfort",canvas,"F  REDUCED EFFECTS     /     M  MUTE",0,14,850,20,12,anchors=bottom)
score_root=rect("Score Popups",canvas,0,0,0,0,((0,0),(1,1)))

start=rect("Start Screen",canvas,0,0,760,430)
image(start,(.013,.028,.06,.97),1)
image(rect("Start Accent",start,0,215,760,3),PALETTE["Cyan"])
text("Title",start,"SPACE ATTACK",0,125,700,94,60,PALETTE["White"],bold=True)
text("Instructions",start,"A / D or arrow keys to move\nHold SPACE to fire\nESC for settings",0,0,660,100,21)
start_button=button("Start Button",start,"START   /   ENTER",-130)

over=rect("Game Over Screen",canvas,0,0,800,500)
over["active"]=0
image(over,(.02,.025,.055,.98),1)
image(rect("Game Over Accent",over,0,250,800,3),PALETTE["Red"])
text("Lost",over,"TRANSMISSION ENDED",0,177,720,40,17,PALETTE["Red"])
text("Game Over Title",over,"GAME OVER",0,105,750,80,57,PALETTE["White"],bold=True)
final=text("Final Score",over,"000000",0,12,720,76,51,PALETTE["Cyan"],bold=True)
results=text("Results",over,"",0,-75,740,70,18)
restart=button("Restart Button",over,"RESTART   /   R OR ENTER",-174)
hud=d.mono(canvas,"HUDView",''.join(f"  {name}: {{fileID: {val}}}\n" for name,val in [("scoreText",score),("bestText",best),("stageText",stage),("healthText",health),("energyFill",energy_fill["transform"]),("settingsView","$SETTINGS_VIEW"),("statusText",status),("resultsText",results),("finalScoreText",final),("startPanel",start["id"]),("gameOverPanel",over["id"]),("startButton",start_button),("restartButton",restart)]))
game=d.mono(systems,"GameController",f"  config: {ref('Assets/SpaceAttack/Settings/Game.asset')}\n  player: {{fileID: {player_control}}}\n  waves: {{fileID: {wave}}}\n  playfield: {{fileID: {bounds}}}\n  hud: {{fileID: {hud}}}\n  projectilesRoot: {{fileID: {projectiles['transform']}}}\n")
flash_node=rect("Damage Flash",canvas,0,0,0,0,((0,0),(1,1)))
flash_image=image(flash_node,(1,.15,.2,0))
feedback=d.mono(systems,"FeedbackController",f"  game: {{fileID: {game}}}\n  view: {{fileID: {camera['transform']}}}\n  effectsRoot: {{fileID: {effects['transform']}}}\n  scoreRoot: {{fileID: {score_root['transform']}}}\n  starfield: {{fileID: {decor['transform']}}}\n  sparkPrefab: {ref('Assets/SpaceAttack/Prefabs/Spark.prefab',spark_id)}\n  screenFlash: {{fileID: {flash_image}}}\n  comfortText: {{fileID: {comfort}}}\n")

settings_panel=rect("Settings Overlay",canvas,0,0,0,0,((0,0),(1,1)))
settings_panel["active"]=0
image(settings_panel,(.005,.01,.025,.85),1)
settings_card=rect("Settings Card",settings_panel,0,0,800,680)
image(settings_card,(.013,.028,.06,1),1)
image(rect("Settings Accent",settings_card,0,340,800,3),PALETTE["Cyan"])
settings_title=text("Settings Title",settings_card,"SETTINGS",0,277,700,50,30,PALETTE["White"],bold=True)
sliders=[]; slider_values=[]
for index,label in enumerate(["Screen shake","Ship hit flash","Screen flash","Explosion particles","Star movement","Invincibility blink","Master volume","Sound effects volume"]):
    y=205-index*52
    text(label+" Label",settings_card,label,-200,y,270,30,18,align=3)
    control=rect(label+" Slider",settings_card,80,y,270,32)
    image(control,PALETTE["Dark"],1)
    image(rect("Track",control,0,0,270,8),PALETTE["Line"])
    fill_area=rect("Fill Area",control,0,0,-18,8,((0,.5),(1,.5)))
    fill=rect("Fill",fill_area,0,0,0,0,((0,0),(1,1)))
    image(fill,PALETTE["Cyan"])
    handle_area=rect("Handle Area",control,0,0,-18,0,((0,0),(1,1)))
    handle=rect("Handle",handle_area,0,0,16,0)
    handle_image=image(handle,PALETTE["White"],1)
    sliders.append(d.mono(control,"Slider",f"  m_Navigation:\n    m_Mode: 0\n  m_Transition: 0\n  m_Interactable: 1\n  m_TargetGraphic: {{fileID: {handle_image}}}\n  m_FillRect: {{fileID: {fill['transform']}}}\n  m_HandleRect: {{fileID: {handle['transform']}}}\n  m_Direction: 0\n  m_MinValue: 0\n  m_MaxValue: 1\n  m_WholeNumbers: 0\n  m_Value: 1\n  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n",True))
    slider_values.append(text(label+" Value",settings_card,"100%",285,y,80,30,18,PALETTE["White"],5))
text("Settings Shortcuts",settings_card,"F - REDUCED EFFECTS    |    M - MUTE",0,-225,700,24,14)
defaults_button=button("Defaults Button",settings_card,"RESET DEFAULTS",-282,x=-175,width=280,height=48,size=17)
close_button=button("Close Settings Button",settings_card,"CLOSE   /   ESC",-282,x=175,width=280,height=48,size=17)
settings_view=d.mono(canvas,"SettingsView",f"  panel: {{fileID: {settings_panel['id']}}}\n  feedback: {{fileID: {feedback}}}\n  openButton: {{fileID: {settings_button}}}\n  closeButton: {{fileID: {close_button}}}\n  defaultsButton: {{fileID: {defaults_button}}}\n  title: {{fileID: {settings_title}}}\n  sliders:\n"+''.join(f"  - {{fileID: {s}}}\n" for s in sliders)+"  values:\n"+''.join(f"  - {{fileID: {v}}}\n" for v in slider_values))
event=d.node("EventSystem",root)
d.mono(event,"EventSystem","  m_FirstSelected: {fileID: 0}\n  m_sendNavigationEvents: 0\n  m_DragThreshold: 10\n",True)
d.mono(event,"StandaloneInputModule","  m_HorizontalAxis: Horizontal\n  m_VerticalAxis: Vertical\n  m_SubmitButton: Submit\n  m_CancelButton: Cancel\n  m_InputActionsPerSecond: 10\n  m_RepeatDelay: 0.5\n  m_ForceModuleActive: 0\n",True)
write("Assets/SpaceAttack/Scenes/Game.unity",d.render(scene=True).replace("$SETTINGS_VIEW",str(settings_view)))

write("ProjectSettings/EditorBuildSettings.asset",HEADER+"--- !u!1045 &1\nEditorBuildSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Scenes:\n  - enabled: 1\n    path: Assets/SpaceAttack/Scenes/Game.unity\n    guid: "+guid("Assets/SpaceAttack/Scenes/Game.unity")+"\n  m_configObjects: {}\n")
write("ProjectSettings/TagManager.asset",HEADER+"--- !u!78 &1\nTagManager:\n  serializedVersion: 2\n  tags: []\n  layers:\n"+''.join("  - "+name+"\n" for name in ["Default","TransparentFX","Ignore Raycast","","Water","UI","","","Player","Enemy","PlayerProjectile","EnemyProjectile"]+[""]*20)+"  m_SortingLayers:\n  - name: Default\n    uniqueID: 0\n    locked: 0\n  m_RenderingLayers:\n  - Default\n")
write("ProjectSettings/TimeManager.asset",HEADER+"--- !u!5 &1\nTimeManager:\n  m_ObjectHideFlags: 0\n  Fixed Timestep: 0.016666668\n  Maximum Allowed Timestep: 0.1\n  m_TimeScale: 1\n  Maximum Particle Timestep: 0.03\n")
write("ProjectSettings/ProjectSettings.asset",HEADER+"--- !u!129 &1\nPlayerSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 28\n  companyName: Local Prototype\n  productName: Space Attack\n  defaultScreenWidth: 1280\n  defaultScreenHeight: 900\n  defaultIsNativeResolution: 0\n  runInBackground: 0\n  colorSpace: 0\n  usePlayerLog: 1\n  activeInputHandler: 0\n  scriptingBackend:\n    Standalone: 0\n  apiCompatibilityLevelPerPlatform:\n    Standalone: 6\n")
axes=""
for name,negative,positive,altnegative,altpositive in [("Horizontal","left","right","a","d"),("Vertical","down","up","s","w"),("Submit","","return","","space"),("Cancel","","escape","","")]:
    axes+=f"  - serializedVersion: 3\n    m_Name: {name}\n    descriptiveName: \n    descriptiveNegativeName: \n    negativeButton: {negative}\n    positiveButton: {positive}\n    altNegativeButton: {altnegative}\n    altPositiveButton: {altpositive}\n    gravity: 3\n    dead: 0.001\n    sensitivity: 3\n    snap: 1\n    invert: 0\n    type: 0\n    axis: 0\n    joyNum: 0\n"
write("ProjectSettings/InputManager.asset",HEADER+"--- !u!13 &1\nInputManager:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Axes:\n"+axes)

def write_metas():
    for target in sorted((OUTPUT/"Assets").rglob("*")):
        if target.suffix==".meta": continue
        path=target.relative_to(OUTPUT).as_posix()
        meta=Path(str(target)+".meta")
        if meta.exists(): continue
        content=f"fileFormatVersion: 2\nguid: {guid(path)}\n"
        if target.is_dir():
            content+="folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        elif target.suffix==".cs":
            content+="MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        else:
            importer={".prefab":"PrefabImporter",".asset":"NativeFormatImporter",".mat":"NativeFormatImporter",".shader":"ShaderImporter",".asmdef":"AssemblyDefinitionImporter"}.get(target.suffix,"DefaultImporter")
            content+=f"{importer}:\n  externalObjects: {{}}\n"
            if target.suffix in (".asset",".mat"): content+=f"  mainObjectFileID: {11400000 if target.suffix=='.asset' else 2100000}\n"
            content+="  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        meta.write_text(content,encoding="utf-8")

write_metas()
print(f"Authored Game.unity, {len(slots)} formation slots, three prefabs and split config assets.")
