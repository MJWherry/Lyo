"use client";

import {useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import {
    AuthRoutes,
    createAsyncAuthApiClient,
    loginUrl,
    type AsyncAuthApiClient,
    type AuthUser,
    type CreateTokenResponse,
} from "lyo-auth-api-client";
import {createAsyncAuthorizationApiClient, type AsyncAuthorizationApiClient} from "lyo-authorization-api-client";
import {GrantCheckForm, GrantPanel} from "lyo-authorization-components";
import {
    LyoButton,
    LyoDataGridFeatureFlags,
    LyoDataGridProjected,
    LyoMenuItem,
    LyoStack,
    LyoTabSet,
    LyoTextField,
    asLyoQueryClient,
    createLyoColumn,
    useLyoSnackbar,
} from "lyo-web-components";

export type AuthProviderLink = {name: string; displayName: string};

export type LoginPageProps = {
    providers: readonly AuthProviderLink[];
    returnUrl?: string;
    sessionPrefix?: string;
    onPasswordSignIn?: (userName: string, password: string) => Promise<void>;
};

export function LoginPage({providers, returnUrl = "/", sessionPrefix, onPasswordSignIn}: LoginPageProps) {
    const [userName, setUserName] = useState("");
    const [password, setPassword] = useState("");
    return (
        <div className="lyo-auth-page">
            <LyoStack spacing={1}>
                {providers.map((provider) => (
                    <a key={provider.name} href={loginUrl(provider.name, returnUrl, sessionPrefix)}>
                        {provider.displayName}
                    </a>
                ))}
                {onPasswordSignIn ? (
                    <LyoStack spacing={1}>
                        <LyoTextField label="Username" value={userName} onChange={(event) => setUserName(event.target.value)} />
                        <LyoTextField label="Password" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
                        <LyoButton onClick={() => void onPasswordSignIn(userName, password)}>Sign in</LyoButton>
                    </LyoStack>
                ) : null}
            </LyoStack>
        </div>
    );
}

export function ProfilePage({apiClient, auth, authorization, userId}: {apiClient: AsyncApiClient; auth?: AsyncAuthApiClient; authorization?: AsyncAuthorizationApiClient; userId?: string}) {
    const client = useMemo(() => auth ?? createAsyncAuthApiClient(apiClient), [apiClient, auth]);
    const [user, setUser] = useState<AuthUser | null>(null);
    useEffect(() => {
        let cancelled = false;
        void (userId ? client.user(userId) : client.me()).then((response) => {
            if (!cancelled) setUser(response.data ?? null);
        });
        return () => {
            cancelled = true;
        };
    }, [client, userId]);
    return (
        <LyoStack spacing={1}>
            <span>{user?.displayName ?? user?.userName ?? "Signed out"}</span>
            <span>{user?.email ?? ""}</span>
            {authorization && user?.id ? <GrantPanel client={authorization} subject={user.id} allowEdit={false} /> : null}
        </LyoStack>
    );
}

export function TokensPage({apiClient, auth}: {apiClient: AsyncApiClient; auth?: AsyncAuthApiClient}) {
    const client = useMemo(() => auth ?? createAsyncAuthApiClient(apiClient), [apiClient, auth]);
    const snackbar = useLyoSnackbar();
    const [rows, setRows] = useState<Array<{id: string; displayName?: string}>>([]);
    const [name, setName] = useState("");
    const [minted, setMinted] = useState<string | null>(null);

    const reload = async () => {
        const response = await client.tokens.list();
        setRows(response.data ?? []);
    };

    useEffect(() => {
        void reload();
    }, [client]);

    const create = async () => {
        const response = await client.tokens.create({displayName: name || "token", scopes: []});
        const body = response.data as CreateTokenResponse | undefined;
        setMinted(body?.plaintext ?? null);
        snackbar.show("Token created. Copy the secret now.");
        await reload();
    };

    return (
        <LyoStack spacing={1}>
            <LyoTextField label="Name" value={name} onChange={(event) => setName(event.target.value)} />
            <LyoButton onClick={() => void create()}>Create token</LyoButton>
            {minted ? <span>{minted}</span> : null}
            {rows.map((row) => (
                <LyoStack key={row.id} direction="row" spacing={1}>
                    <span>{row.displayName ?? row.id}</span>
                    <LyoButton size="small" onClick={() => void client.tokens.revoke(row.id).then(reload)}>
                        Revoke
                    </LyoButton>
                </LyoStack>
            ))}
        </LyoStack>
    );
}

export function DebugPage({apiClient, auth}: {apiClient: AsyncApiClient; auth?: AsyncAuthApiClient}) {
    const client = useMemo(() => auth ?? createAsyncAuthApiClient(apiClient), [apiClient, auth]);
    const [text, setText] = useState("");
    useEffect(() => {
        void client.session().then((response) => setText(JSON.stringify(response.data ?? null, null, 2)));
    }, [client]);
    return <pre>{text}</pre>;
}

export function AuthAdminPanel({apiClient}: {apiClient: AsyncApiClient}) {
    const auth = useMemo(() => createAsyncAuthApiClient(apiClient), [apiClient]);
    const grants = useMemo(() => createAsyncAuthorizationApiClient(apiClient), [apiClient]);
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const snackbar = useLyoSnackbar();
    const grid = (route: string, key: string, hardDelete = false) => (
        <LyoDataGridProjected<Record<string, unknown>>
            apiClient={query}
            gridKey={key}
            route={route}
            columns={[createLyoColumn({id: "id", field: "Id", header: "Id"}), createLyoColumn({id: "name", field: "DisplayName", header: "Name", quickSearch: true})]}
            keySelector={(row) => [row.Id ?? row.id]}
            features={LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.Filterable}
            rowMenu={
                hardDelete
                    ? (row) => (
                          <LyoMenuItem
                              onClick={() =>
                                  void auth.admin.tokens.delete(String(row.Id ?? row.id)).then(() => snackbar.show("Token deleted."))
                              }
                          >
                              Delete
                          </LyoMenuItem>
                      )
                    : undefined
            }
        />
    );
    return (
        <LyoTabSet
            persistKey="auth-admin"
            tabs={[
                {id: "users", text: "Users", content: grid(AuthRoutes.user, "AuthUsers")},
                {id: "tokens", text: "Tokens", content: grid(AuthRoutes.token, "AuthTokens", true)},
                {id: "claims", text: "Claims", content: grid(AuthRoutes.claim, "AuthClaims")},
                {id: "scopes", text: "Scopes", content: grid(AuthRoutes.scope, "AuthScopes")},
                {id: "identities", text: "Identities", content: grid(AuthRoutes.linkedIdentity, "AuthIdentities")},
                {id: "events", text: "Audit", content: grid(AuthRoutes.event, "AuthEvents")},
                {
                    id: "grants",
                    text: "Grants",
                    content: (
                        <LyoStack spacing={2}>
                            <GrantPanel client={grants} allowEdit />
                            <GrantCheckForm client={grants} />
                        </LyoStack>
                    ),
                },
            ]}
        />
    );
}
