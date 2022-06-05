define(["loading", "dialogHelper", "formDialogStyle", "emby-checkbox", "emby-select", "emby-toggle"],
    function(loading, dialogHelper) {

        var pluginId = "D9085D73-7142-4D82-905B-2A0B1949A6D2";

        return function(view) {
            view.addEventListener('viewshow',
                async () => {
                    var chkAutoMute = view.querySelector('#autoMute');
                    var startOffset = view.querySelector('#startOffset');
                    var endOffset = view.querySelector('#endOffset');

                    ApiClient.getPluginConfiguration(pluginId).then((config) => {
                        chkAutoMute.checked = config.EnablePottyMouth ?? false;
                        startOffset.value = config.startOffset ? config.startOffset : 0;
                        endOffset.value = config.endOffset ? config.endOffset : 0;
                    });

                    chkAutoMute.addEventListener('change', (elem) => {
                        elem.preventDefault();
                        var autoMute = chkAutoMute.checked;
                        enableAutoMute(autoMute);
                    });

                    startOffset.addEventListener('change', (elem) => {
                        elem.preventDefault();
                        setStartOffset();
                    });

                    endOffset.addEventListener('change', (elem) => {
                        elem.preventDefault();
                        setEndOffset();
                    });

                    function enableAutoMute(autoMute) {
                        ApiClient.getPluginConfiguration(pluginId).then((config) => {
                            config.EnablePottyMouth = autoMute;
                            ApiClient.updatePluginConfiguration(pluginId, config).then(() => {});
                        });
                    }

                    function setStartOffset() {
                        ApiClient.getPluginConfiguration(pluginId).then((config) => {
                            config.startOffset = startOffset.value;
                            ApiClient.updatePluginConfiguration(pluginId, config).then(() => { });
                        });
                    }

                    function setEndOffset() {
                        ApiClient.getPluginConfiguration(pluginId).then((config) => {
                            config.endOffset = endOffset.value;
                            ApiClient.updatePluginConfiguration(pluginId, config).then(() => { });
                        });
                    }
                });
        }
    });