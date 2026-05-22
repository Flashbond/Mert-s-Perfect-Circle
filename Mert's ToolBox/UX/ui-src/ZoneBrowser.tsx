import React, { useState, useEffect } from "react";
import { bindValue, useValue, trigger } from "cs2/api";

const zoneListJson$ = bindValue<string>("MertsToolBox", "ZoneListJson", "[]");

interface MertsZoneBrowserProps {
    isOpen: boolean;
    onClose: () => void;
}

interface GroupedZones {
    [key: string]: any[];
}

export const MertsZoneBrowser: React.FC<MertsZoneBrowserProps> = ({ isOpen, onClose }) => {
    const rawJson = useValue(zoneListJson$);
    const [groupedZones, setGroupedZones] = useState<GroupedZones>({});
    const [selectedZone, setSelectedZone] = useState<string>("");

    useEffect(() => {
        if (!rawJson) return;

        try {
            const rawList: any[] = JSON.parse(rawJson);

            // 1. Yoğunluk Derecesi Hesaplama (Grup içi doğru sıralama için)
            const getDensityOrder = (name: string): number => {
                const upperName = name.toUpperCase();
                if (!upperName.includes("LOW") && !upperName.includes("MEDIUM") && !upperName.includes("HIGH") && !upperName.includes("MIXED")) return 0;
                if (upperName.includes("LOW")) return 1;
                if (upperName.includes("MEDIUM") || upperName.includes("MIXED")) return 2;
                if (upperName.includes("HIGH")) return 3;
                return 4;
            };

            // 2. Makro Kategorilere Göre Doğrudan Sepetleme (Göz karıştırmayan gruplama)
            const groups: GroupedZones = {
                "RESIDENTIAL ZONES": [],
                "COMMERCIAL ZONES": [],
                "OFFICE ZONES": [],
                "INDUSTRIAL ZONES": [],
                "OTHER ZONES": []
            };

            rawList.forEach(zone => {
                const upperName = zone.name.toUpperCase();
                if (upperName.includes("RESIDENTIAL")) groups["RESIDENTIAL ZONES"].push(zone);
                else if (upperName.includes("COMMERCIAL")) groups["COMMERCIAL ZONES"].push(zone);
                else if (upperName.includes("OFFICE")) groups["OFFICE ZONES"].push(zone);
                else if (upperName.includes("INDUSTRIAL")) groups["INDUSTRIAL ZONES"].push(zone);
                else groups["OTHER ZONES"].push(zone);
            });

            // 3. Her sepeti kendi içinde yoğunluğa ve alfabetik sıraya göre diz
            Object.keys(groups).forEach(key => {
                groups[key].sort((a, b) => {
                    const densA = getDensityOrder(a.name);
                    const densB = getDensityOrder(b.name);
                    if (densA !== densB) return densA - densB;
                    return a.name.localeCompare(b.name);
                });
            });

            setGroupedZones(groups);

        } catch (e) {
            console.error("Gruplama motoru kriz çıkardı:", e);
        }
    }, [rawJson]);

    if (!isOpen) return null;

    const browserBg = "rgba(20, 26, 30, 0.92)";
    const cardBgActive = "rgba(255, 255, 255, 0.15)";
    const cardBgInactive = "rgba(0, 0, 0, 0.4)";

    // Seçili zon adını grupların içinde arayıp bulalım
    let selectedZoneName = "SELECT A ZONE";
    Object.values(groupedZones).forEach(list => {
        const found = list.find(z => z.id === selectedZone);
        if (found) selectedZoneName = found.name;
    });

    return (
        <div style={{
            position: "fixed",
            // İstediğin gibi: İki tık sağa, iki tık yukarı konumu (%42 -> %38 dikey, %58 -> %60 yatay)
            top: "40%",
            left: "55%",
            transform: "translate(-50%, -50%)",
            width: "820rem",
            backgroundColor: browserBg,
            backdropFilter: "blur(15rem)",
            border: "1rem solid rgba(255, 255, 255, 0.15)",
            borderRadius: "12rem",
            boxShadow: "0 25rem 60rem rgba(0,0,0,0.85)",
            zIndex: 10000,
            display: "flex",
            flexDirection: "column",
            padding: "20rem"
        }}>
            {/* HEADER */}
            <div style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                borderBottom: "1rem solid rgba(255,255,255,0.1)",
                paddingBottom: "12rem",
                marginBottom: "10rem"
            }}>
                <div style={{ display: "flex", flexDirection: "column" }}>
                    <span style={{ fontSize: "11rem", color: "#8a979f", letterSpacing: "1rem" }}>
                        ZONE DATABASE BROWSER
                    </span>
                    <span style={{ fontSize: "16rem", fontWeight: "bold", color: "#5bbf74", marginTop: "2rem" }}>
                        {selectedZoneName.toUpperCase()}
                    </span>
                </div>

                <button
                    onClick={onClose}
                    style={{
                        background: "rgba(231, 76, 60, 0.2)",
                        border: "1rem solid rgba(231, 76, 60, 0.5)",
                        color: "#fff",
                        borderRadius: "6rem",
                        padding: "6rem 16rem",
                        cursor: "pointer",
                        fontWeight: "bold"
                    }}
                >
                    X
                </button>
            </div>

            {/* SCROLLABLE CONTAINER */}
            <div style={{ maxHeight: "520rem", overflowY: "auto", paddingRight: "6rem" }}>
                {Object.keys(groupedZones).map(categoryTitle => {
                    const list = groupedZones[categoryTitle];
                    if (list.length === 0) return null; // Boş kategorileri basma

                    return (
                        <div key={categoryTitle} style={{ marginBottom: "16rem", display: "flex", flexDirection: "column" }}>
                            {/* KATEGORİ AYRACINDAN BAŞLIK */}
                            <div style={{
                                fontSize: "11rem",
                                fontWeight: "bold",
                                color: "#5bbf74",
                                backgroundColor: "rgba(91, 191, 116, 0.08)",
                                padding: "4rem 8rem",
                                borderRadius: "4rem",
                                marginBottom: "8rem",
                                letterSpacing: "0.5rem"
                            }}>
                                {categoryTitle}
                            </div>

                            {/* KATEGORİ İÇİ KUTULAR (FLEX ROW) */}
                            <div style={{ display: "flex", flexDirection: "row", flexWrap: "wrap", alignItems: "flex-start" }}>
                                {list.map((zone) => {
                                    const isSelected = selectedZone === zone.id;
                                    return (
                                        <button
                                            key={zone.id}
                                            onClick={() => {
                                                setSelectedZone(zone.id);
                                                trigger("MertsToolBox", "SelectZoneFromBrowser", zone.id);
                                            }}
                                            style={{
                                                display: "flex",
                                                flexDirection: "row",
                                                alignItems: "center",
                                                width: "32%", // Tam 3 sütun düzeni korundu
                                                marginRight: "1%",
                                                marginBottom: "8rem",
                                                padding: "6rem 10rem",
                                                backgroundColor: isSelected ? cardBgActive : cardBgInactive,
                                                border: isSelected ? "1rem solid #5bbf74" : "1rem solid transparent",
                                                borderRadius: "8rem",
                                                cursor: "pointer",
                                                boxSizing: "border-box"
                                            }}
                                        >
                                            {/* İKON VE BÖLGE BADGE'I HİZALAMASI (OYUNDAKİ GİBİ ÜST ÜSTE) */}
                                            <div style={{ position: "relative", width: "42rem", height: "42rem", flexShrink: 0 }}>
                                                <img
                                                    src={zone.icon && zone.icon.startsWith("Media/") ? zone.icon : "Media/Game/Icons/ZoneResidentialLow.svg"}
                                                    style={{ width: "100%", height: "100%", pointerEvents: "none" }}
                                                    onError={(e) => { (e.target as HTMLImageElement).src = "Media/Game/Icons/ZoneResidentialLow.svg"; }}
                                                />

                                                {zone.theme && (
                                                    <img
                                                        src={zone.theme}
                                                        style={{
                                                            position: "absolute",
                                                            // İki tık yukarı, iki tık sağa kaydırarak ikonun sağ alt köşesine tam oturttuk
                                                            bottom: "1rem",
                                                            right: "1rem",
                                                            width: "18rem",
                                                            height: "18rem",
                                                            pointerEvents: "none",
                                                            filter: "drop-shadow(0px 2px 3px rgba(0,0,0,0.9))"
                                                        }}
                                                        onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }}
                                                    />
                                                )}
                                            </div>

                                            {/* YANDAKİ TEMİZLENMİŞ İSİM */}
                                            <div style={{
                                                marginLeft: "12rem",
                                                fontSize: "13rem",
                                                color: isSelected ? "#5bbf74" : "#e0e0e0",
                                                textAlign: "left",
                                                overflow: "hidden",
                                                textOverflow: "ellipsis",
                                                whiteSpace: "nowrap"
                                            }}>
                                                {zone.name.replace("EU ", "").replace("NA ", "")}
                                            </div>
                                        </button>
                                    );
                                })}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};