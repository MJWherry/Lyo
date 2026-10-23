import {describe, expect, it} from "vitest";
import type {StoryChoice, StoryGraph} from "lyo-story-api-client";
import {choiceEdge, layoutStory, toStoryBuilderCsharp, validateStory, NODE_WIDTH, COLUMN_GAP} from "./storyGraph.js";

function goto(target: string): StoryGraph {
    return {
        title: "Door",
        slug: "door",
        startNodeKey: "vault_door",
        nodes: [
            {key: "vault_door", choices: [{key: "go", effects: [{kind: "gotoNode", targetNodeKey: target}]}]},
            {key: "inside"},
        ],
    };
}

const heist: StoryGraph = {
    title: "The Heist",
    slug: "the-heist",
    startNodeKey: "vault_door",
    nodes: [
        {key: "vault_door", choices: [{key: "pick_lock", effects: [{kind: "gotoNode", targetNodeKey: "inside"}]}]},
        {key: "inside"},
        {key: "loose"},
    ],
};

describe("validateStory", () => {
    it("validateStory_UnknownGoto_ReportsTarget", () => {
        expect(validateStory(goto("missing")).join(" ")).toContain("missing");
        expect(validateStory(goto("inside")).join(" ")).not.toContain("Goto");
    });

    it("validateStory_UnknownAction_ReportsInvoke", () => {
        const draft = goto("inside");
        draft.nodes![0]!.choices![0]!.effects!.push({kind: "invokeAction", invokeActionKey: "nope"});
        expect(validateStory(draft).join(" ")).toContain("nope");
    });

    it("validateStory_DuplicateNodeKey_ReportsKey", () => {
        const draft: StoryGraph = {title: "Dup", slug: "dup", nodes: [{key: "vault"}, {key: "vault"}]};
        expect(validateStory(draft).join(" ")).toContain("vault");
    });
});

describe("layoutStory", () => {
    it("layoutStory_HeistShape_PlacesStartThenBranches", () => {
        const layout = layoutStory(heist);
        const start = layout.nodes.find((node) => node.key === "vault_door")!;
        const inside = layout.nodes.find((node) => node.key === "inside")!;
        expect(start.column).toBe(0);
        expect(inside.column).toBe(start.column + 1);
        expect(inside.x).toBe(start.x + NODE_WIDTH + COLUMN_GAP);
        expect(inside.x).toBe(220 + 96);
        expect(layout.nodes.some((node) => node.key === "loose")).toBe(true);
    });

    it("layoutStory_Cycle_ReportsNodesAndOmitsThem", () => {
        const layout = layoutStory({
            title: "Loop",
            slug: "loop",
            startNodeKey: "a",
            nodes: [
                {key: "a", choices: [{key: "to_b", effects: [{kind: "gotoNode", targetNodeKey: "b"}]}]},
                {key: "b", choices: [{key: "to_a", effects: [{kind: "gotoNode", targetNodeKey: "a"}]}]},
            ],
        });
        expect([...layout.cyclic].sort()).toEqual(["a", "b"]);
        expect(layout.nodes).toEqual([]);
        expect(layout.edges).toEqual([]);
    });
});

describe("choiceEdge", () => {
    it("choiceEdge_LastGoto_Wins", () => {
        const choice: StoryChoice = {
            key: "go",
            effects: [
                {kind: "set", variableKey: "alarm", value: "true"},
                {kind: "gotoNode", targetNodeKey: "inside"},
                {kind: "gotoNode", targetNodeKey: "guards"},
            ],
        };
        const edge = choiceEdge(choice);
        expect(edge.toKey).toBe("guards");
        expect(edge.targetNodeKey).toBe("guards");
        const layout = layoutStory({
            title: "Door",
            slug: "door",
            startNodeKey: "vault",
            nodes: [{key: "vault", choices: [choice]}, {key: "inside"}, {key: "guards"}],
        });
        expect(layout.edges.map((item) => item.toKey)).toEqual(["guards"]);
    });
});

describe("toStoryBuilderCsharp", () => {
    it("toStoryBuilderCsharp_HeistShape_EmitsStartNodeAndWhenAll", () => {
        const text = toStoryBuilderCsharp({
            title: "The Heist",
            slug: "the-heist",
            startNodeKey: "vault_door",
            variables: [{key: "alarm", type: "boolean", defaultValue: "false"}],
            nodes: [
                {
                    key: "vault_door",
                    choices: [
                        {
                            key: "pick_lock",
                            prompt: "Pick the lock",
                            enabledWhen: {
                                $type: "group",
                                Operator: "And",
                                Children: [{$type: "condition", Field: "var.has_lockpick", Comparison: "Equals", Value: true}],
                            },
                        },
                    ],
                },
            ],
        });
        expect(text).toContain('StoryBuilder.New("The Heist")');
        expect(text).toContain('.Slug("the-heist")');
        expect(text).toContain("StoryVariableType.Boolean");
        expect(text).toContain('.StartNode("vault_door"');
        expect(text).toContain(".WhenAll(");
    });
});
