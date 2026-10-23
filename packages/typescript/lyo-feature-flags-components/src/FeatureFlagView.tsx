"use client";

import {useEffect, useState, type ReactNode} from "react";
import type {AsyncFeatureFlagApiClient} from "lyo-feature-flags-api-client";
import {isFeatureEnabled} from "./isFeatureEnabled.js";

export type FeatureFlagViewProps = {
    client: AsyncFeatureFlagApiClient;
    name: string;
    children?: ReactNode;
};

export function FeatureFlagView({client, name, children}: FeatureFlagViewProps) {
    const [names, setNames] = useState<string[] | null>(null);

    useEffect(() => {
        let cancelled = false;
        void client.enabled().then((response) => {
            if (cancelled)
                return;
            setNames(response.ok && response.data ? response.data : []);
        });
        return () => {
            cancelled = true;
        };
    }, [client, name]);

    if (names === null || !isFeatureEnabled(names, name))
        return null;

    return <>{children}</>;
}
